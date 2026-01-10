using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MountainRescueApp.Models;
using MountainRescueApp.Services;
using Firebase.Database;
using Firebase.Database.Query;

namespace MountainRescueApp;

public partial class AllTouristsMapPage : ContentPage
{
    private readonly UserModel selectedUser;

    private IDisposable selectedUserSubscription;
    private IDisposable allUsersSubscription;

    private Polyline selectedUserPolyline;
    private Pin selectedUserPin;

    private UInt64 lastLoadedLocationNo = 0;

    // Pentru ceilalți turiști
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
    // 1. ÎNCĂRCARE COMPLETĂ TURIST SELECTAT
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

        MainMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(lastPos, Distance.FromMeters(200))
        );
    }

    // ---------------------------------------------------------
    // 2. ÎNCĂRCARE COMPLETĂ PENTRU TOȚI CEILALȚI TURIȘTI
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

            // Pin
            var pin = new Pin
            {
                Label = user.Name,
                Address = "Ultima locație",
                Location = lastPos,
                Type = PinType.Place
            };

            otherUsersPins[user.CNP] = pin;
            MainMap.Pins.Add(pin);

            // Traseu
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
    // 3. REAL-TIME TURIST SELECTAT
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

        selectedUserPolyline.Geopath.Add(position);

        MainMap.Pins.Remove(selectedUserPin);
        selectedUserPin.Location = position;
        MainMap.Pins.Add(selectedUserPin);

        MainMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200))
        );
    }

    // ---------------------------------------------------------
    // 4. REAL-TIME PENTRU TOȚI CEILALȚI TURIȘTI
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
        {
            MainMap.Pins.Remove(pin);
            pin.Location = position;
            MainMap.Pins.Add(pin);
        }

        if (otherUsersPolylines.TryGetValue(cnp, out var poly))
        {
            poly.Geopath.Add(position);
        }
    }
}
