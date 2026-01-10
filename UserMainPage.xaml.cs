
using MountainRescueApp.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Timers;
using MountainRescueApp.Services;
using System.Threading; // for SemaphoreSlim
using System.Diagnostics;

namespace MountainRescueApp;

public partial class UserMainPage : ContentPage
{
    private System.Timers.Timer _timer;
    private bool _isTracking = false;
    private Polyline _userPath;
    private readonly SemaphoreSlim _tickLock = new(1, 1);

    private readonly LocationModel Userlocation = new();
    private UserModel user_global = new();
    private ulong No_Location = 0; // use ulong for UInt64 alias

    public UserMainPage(UserModel user)
    {
        InitializeComponent();

        _userPath = new Polyline { StrokeColor = Colors.Red, StrokeWidth = 15 };
        user_global = user;

        // Attach polyline to the map
        mappy.MapElements.Add(_userPath);

        // Ensure Emergency button is hidden initially
        EmergencyButton.IsVisible = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) return;

        var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
        if (location == null) return;

        mappy.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(location.Latitude, location.Longitude),
            Distance.FromKilometers(1)));

        mappy.MapType = MapType.Satellite;
    }

    private async void TrackButton_Clicked(object sender, EventArgs e)
    {
        if (_isTracking)
        {
            StopTracking();
            TrackButton.Text = "Start Tracking";
            EmergencyButton.IsVisible = false;
            await UserRepository.UpdateTrack(user_global, false);
        }
        else
        {
            No_Location = 0;

            // Clear previous path points in DB for this user
            await LocationRepository.Delete(user_global.CNP);

            // Save new state
            user_global.Track = true;
            await UserRepository.UpdateTrack(user_global, true);

            _userPath.Geopath.Clear();

            await StartTracking();

            TrackButton.Text = "Stop Tracking";
            EmergencyButton.IsVisible = true;
        }
    }

    private async Task StartTracking()
    {
        _isTracking = true;

        _timer = new System.Timers.Timer(5000)
        {
            AutoReset = true,
            Enabled = true
        };
        _timer.Elapsed += async (s, e) => await UpdateLocation();

        // First point immediately
        await UpdateLocation();
    }

    private void StopTracking()
    {
        _isTracking = false;
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private async Task UpdateLocation()
    {
        // Prevent overlapping ticks
        if (!await _tickLock.WaitAsync(0)) return;

        try
        {
            // Check server-side Track before doing anything
            var trackOn = await UserRepository.GetTrackByCnpAsync(user_global.CNP);
            if (!trackOn)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StopTracking();
                    TrackButton.Text = "Start Tracking";
                    EmergencyButton.IsVisible = false;
                });
                return;
            }

            var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
            if (location == null) return;

            var position = new Location(location.Latitude, location.Longitude);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                mappy.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(50)));
                _userPath.Geopath.Add(position);

                Userlocation.LocationNo = No_Location;
                Userlocation.Longitude = location.Longitude;
                Userlocation.Latitude = location.Latitude;
                Userlocation.CNP = user_global.CNP;

                await LocationRepository.Save(Userlocation, user_global.CNP, No_Location);
                No_Location++;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UpdateLocation error: {ex}");
        }
        finally
        {
            _tickLock.Release();
        }
    }

    private async void EmergencyButton_Clicked(object sender, EventArgs e)
    {
        if (!_isTracking) return;
        await Navigation.PushAsync(new EmergencyFormPage(user_global));
    }
}
