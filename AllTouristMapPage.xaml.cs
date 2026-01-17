using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MountainRescueApp.Models;
using MountainRescueApp.Services;
using Firebase.Database;
using Firebase.Database.Query;
using System.Diagnostics;

namespace MountainRescueApp;

public partial class AllTouristsMapPage : ContentPage
{
    private readonly UserModel selectedUser;

    private IDisposable selectedUserSubscription;
    private IDisposable allUsersSubscription;

    private Polyline selectedUserPolyline;
    private Pin selectedUserPin;

    private ulong lastLoadedLocationNo = 0;

    private Location lastCenter = null;
    private Location lastSelectedPoint = null;

    private Dictionary<string, Pin> otherUsersPins = new();
    private Dictionary<string, Polyline> otherUsersPolylines = new();
    private Dictionary<string, ulong> otherUsersLastLocationNo = new();

    public AllTouristsMapPage(UserModel selected)
    {
        InitializeComponent();
        selectedUser = selected;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadFullTrackSelectedUser();
        await LoadAllOtherTourists();

        StartRealtimeTrackingSelectedUser();
        StartRealtimeTrackingAllTourists();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        selectedUserSubscription?.Dispose();
        allUsersSubscription?.Dispose();
    }

    // ---------------------------------------------------------
    // 1. LOAD FULL TRACK FOR SELECTED USER
    // ---------------------------------------------------------
    private async Task LoadFullTrackSelectedUser()
    {
        var path = await LocationRepository.GetByCNP(selectedUser.CNP);
        var userColor = Color.FromRgb(selectedUser.Red, selectedUser.Green, selectedUser.Blue);

        if (path == null || path.Count == 0)
            return;

        selectedUserPolyline = new Polyline
        {
            StrokeColor = userColor,
            StrokeWidth = 15
        };

        foreach (var loc in path)
        {
            selectedUserPolyline.Geopath.Add(new Location(loc.Latitude, loc.Longitude));
            lastLoadedLocationNo = loc.LocationNo;
        }

        MainMap.MapElements.Add(selectedUserPolyline);

        var last = path.Last();
        var lastPos = new Location(last.Latitude, last.Longitude);

        selectedUserPin = new Pin
        {
            Label = selectedUser.Name,
            Address = "Ultima locație cunoscută",
            Location = lastPos,
            Type = PinType.Place
        };

        MainMap.Pins.Add(selectedUserPin);

        lastCenter = lastPos;
        lastSelectedPoint = lastPos;

        MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(lastPos, Distance.FromMeters(200)));
    }

    // ---------------------------------------------------------
    // 2. LOAD ALL OTHER TOURISTS
    // ---------------------------------------------------------
    private async Task LoadAllOtherTourists()
    {
        var allUsers = await UserRepository.GetAllUsers();

        foreach (var user in allUsers)
        {
            if (user.CNP == selectedUser.CNP)
                continue;

            var userColor = Color.FromRgb(user.Red, user.Green, user.Blue);
            var path = await LocationRepository.GetByCNP(user.CNP);

            if (path == null || path.Count == 0)
                continue;

            var last = path.Last();
            var lastPos = new Location(last.Latitude, last.Longitude);

            var pin = new Pin
            {
                Label = user.Name,
                Address = "Ultima locație",
                Location = lastPos,
                Type = PinType.Place
            };

            otherUsersPins[user.CNP] = pin;
            MainMap.Pins.Add(pin);

            var poly = new Polyline
            {
                StrokeColor = userColor,
                StrokeWidth = 15
            };

            foreach (var loc in path)
                poly.Geopath.Add(new Location(loc.Latitude, loc.Longitude));

            otherUsersPolylines[user.CNP] = poly;
            MainMap.MapElements.Add(poly);

            otherUsersLastLocationNo[user.CNP] = last.LocationNo;
        }
    }

    // ---------------------------------------------------------
    // 3. REAL-TIME SELECTED USER
    // ---------------------------------------------------------
    private void StartRealtimeTrackingSelectedUser()
    {
        selectedUserSubscription?.Dispose();

        selectedUserSubscription = LocationRepository.firebaseClient
            .Child("Locations")
            .Child(selectedUser.CNP)
            .AsObservable<LocationModel>()
            .Subscribe(item =>
            {
                if (item.Object == null)
                    return;

                var loc = item.Object;

                if (loc.LocationNo <= lastLoadedLocationNo)
                    return;

                lastLoadedLocationNo = loc.LocationNo;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateSelectedUserOnMap(loc);
                });
            });
    }

    private void UpdateSelectedUserOnMap(LocationModel loc)
    {
        var position = new Location(loc.Latitude, loc.Longitude);

        // Filter out bad jumps (convert km → meters)
        if (lastSelectedPoint != null)
        {
            double distKm = Location.CalculateDistance(lastSelectedPoint, position, DistanceUnits.Kilometers);
            double dist = distKm * 1000;

            if (dist > 40) // ignore teleport jumps
                return;
        }

        lastSelectedPoint = position;

        selectedUserPolyline.Geopath.Add(position);

        selectedUserPin.Location = position;

        // AUTO-CENTER ALWAYS (Option A)
        MainMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200))
        );

        lastCenter = position;
    }

    // ---------------------------------------------------------
    // 4. REAL-TIME OTHER TOURISTS
    // ---------------------------------------------------------
    private void StartRealtimeTrackingAllTourists()
    {
        allUsersSubscription?.Dispose();

        allUsersSubscription = LocationRepository.firebaseClient
            .Child("Locations")
            .AsObservable<LocationModel>()
            .Subscribe(item =>
            {
                if (item.Object == null)
                    return;

                var cnp = item.Key;
                var loc = item.Object;

                if (cnp == selectedUser.CNP)
                    return;

                if (!otherUsersLastLocationNo.ContainsKey(cnp))
                    otherUsersLastLocationNo[cnp] = 0;

                if (loc.LocationNo <= otherUsersLastLocationNo[cnp])
                    return;

                otherUsersLastLocationNo[cnp] = loc.LocationNo;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateOtherUserOnMap(cnp, loc);
                });
            });
    }

    private void UpdateOtherUserOnMap(string cnp, LocationModel loc)
    {
        var position = new Location(loc.Latitude, loc.Longitude);

        if (otherUsersPins.TryGetValue(cnp, out var pin))
            pin.Location = position;

        if (otherUsersPolylines.TryGetValue(cnp, out var poly))
            poly.Geopath.Add(position);
    }
}
