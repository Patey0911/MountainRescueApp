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
    // 0. EMERGENCY TEXT BUILDER
    // ---------------------------------------------------------
    private string GetEmergencyText(EmergencyModel e)
    {
        if (e == null || !e.Accidentat)
            return "Nu există urgențe active.";

        var list = new List<string>();

        if (e.Pierdut) list.Add("Pierdut");
        if (e.Entorsa) list.Add("Entorsă");
        if (e.Luxatie) list.Add("Luxație");
        if (e.Fractura) list.Add("Fractură");
        if (e.Contuzie) list.Add("Contuzie");
        if (e.Hipotermie) list.Add("Hipotermie");
        if (e.Degeratura) list.Add("Degerătură");
        if (e.Insolatie) list.Add("Insolație");
        if (e.Deshidratare) list.Add("Deshidratare");
        if (e.RaudeAltitudine) list.Add("Rău de altitudine");
        if (e.EpuizareFizica) list.Add("Epuizare fizică");
        if (e.CrizaRespiratorie) list.Add("Criză respiratorie");
        if (e.Avalansa) list.Add("Avalanșă");
        if (e.Intepatura) list.Add("Înțepătură");
        if (e.Muscatura) list.Add("Muşcătură");

        return "Urgențe:\n- " + string.Join("\n- ", list);

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

        var emergency = await EmergenciesRepository.GetByCNP(selectedUser.CNP);

        selectedUserPin = new Pin
        {
            Label = selectedUser.Name,
            Address = GetEmergencyText(emergency),
            Location = lastPos,
            Type = PinType.Place
        };

        selectedUserPin.MarkerClicked += OnPinMarkerClicked;

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

            var emergency = await EmergenciesRepository.GetByCNP(user.CNP);

            var pin = new Pin
            {
                Label = user.Name,
                Address = GetEmergencyText(emergency),
                Location = lastPos,
                Type = PinType.Place
            };

            pin.MarkerClicked += OnPinMarkerClicked;

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

    private async void UpdateSelectedUserOnMap(LocationModel loc)
    {
        var position = new Location(loc.Latitude, loc.Longitude);

        if (lastSelectedPoint != null)
        {
            double distKm = Location.CalculateDistance(lastSelectedPoint, position, DistanceUnits.Kilometers);
            double dist = distKm * 1000;

            if (dist > 40)
                return;
        }

        lastSelectedPoint = position;

        selectedUserPolyline.Geopath.Add(position);
        selectedUserPin.Location = position;

        var emergency = await EmergenciesRepository.GetByCNP(selectedUser.CNP);
        selectedUserPin.Address = GetEmergencyText(emergency);

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

    private async void UpdateOtherUserOnMap(string cnp, LocationModel loc)
    {
        var position = new Location(loc.Latitude, loc.Longitude);

        if (otherUsersPins.TryGetValue(cnp, out var pin))
        {
            pin.Location = position;

            var emergency = await EmergenciesRepository.GetByCNP(cnp);
            pin.Address = GetEmergencyText(emergency);
        }

        if (otherUsersPolylines.TryGetValue(cnp, out var poly))
            poly.Geopath.Add(position);
    }

    // ---------------------------------------------------------
    // 5. PIN CLICK → SALVARE TURIST
    // ---------------------------------------------------------
    private async void OnPinMarkerClicked(object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;

        var pin = sender as Pin;
        if (pin == null)
            return;

        string cnp = null;

        if (pin == selectedUserPin)
            cnp = selectedUser.CNP;
        else
            cnp = otherUsersPins.FirstOrDefault(x => x.Value == pin).Key;

        if (string.IsNullOrEmpty(cnp))
            return;

        // Luăm urgența din Firebase
        var emergency = await EmergenciesRepository.GetByCNP(cnp);

        // Construim textul urgențelor
        string emergencyText = GetEmergencyText(emergency);

        // Afișăm popup-ul complet
        bool save = await DisplayAlert(
            $"Turist: {pin.Label}",
            emergencyText,
            "Salvează turistul",
            "Închide"
        );

        if (!save)
            return;

        await MarkTouristAsSaved(cnp);

        pin.Address = "Ultima locație";

    }

    private async Task MarkTouristAsSaved(string cnp)
    {
        var emergency = new EmergencyModel
        {
            CNP = cnp,
            Pierdut = false,
            Entorsa = false,
            Luxatie = false,
            Fractura = false,
            Contuzie = false,
            Hipotermie = false,
            Degeratura = false,
            Insolatie = false,
            Deshidratare = false,
            RaudeAltitudine = false,
            EpuizareFizica = false,
            CrizaRespiratorie = false,
            Avalansa = false,
            Intepatura = false,
            Muscatura = false,
            Accidentat = false
        };

        await EmergenciesRepository.Save(emergency);
    }
}
