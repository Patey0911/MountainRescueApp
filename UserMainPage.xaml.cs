
using MountainRescueApp.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Timers;
using MountainRescueApp.Services;
using System.Threading;
using System.Diagnostics; // for SemaphoreSlim

namespace MountainRescueApp;

public partial class UserMainPage : ContentPage
{
    private System.Timers.Timer _timer;
    private bool _isTracking = false;
    private Polyline _userPath;
    private readonly SemaphoreSlim _tickLock = new(1, 1); // prevent overlapping ticks

    LocationModel Userlocation = new LocationModel();
    UserModel user_global = new UserModel();
    UInt64 No_Location = 0;

    public UserMainPage(UserModel user)
    {
        InitializeComponent();

        _userPath = new Polyline
        {
            StrokeColor = Colors.Red,
            StrokeWidth = 15
        };

        user_global = user;
        mappy.MapElements.Add(_userPath);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) return;

        var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
        if (location == null) return;

        mappy.MoveToRegion(
            MapSpan.FromCenterAndRadius(
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
            await UserRepository.UpdateTrack(user_global, false);
        }
        else
        {
            No_Location = 0;

            // clear previous path points in DB for this user
            await LocationRepository.Delete(user_global.CNP);

            // save the new state
            user_global.Track = true;
            await UserRepository.UpdateTrack(user_global, true);

            _userPath.Geopath.Clear();

            await StartTracking();
            TrackButton.Text = "Stop Tracking";
        }
    }

    private async Task StartTracking()
    {
        _isTracking = true;

        _timer = new System.Timers.Timer(5000);
        _timer.AutoReset = true;
        _timer.Elapsed += async (s, e) => await UpdateLocation();
        _timer.Enabled = true;

        await UpdateLocation(); // first point immediately
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
        // prevent reentrancy if previous tick still running
        if (!await _tickLock.WaitAsync(0)) return;

        try
        {
            // 1) Check server-side Track before doing anything
            var trackOn = await UserRepository.GetTrackByCnpAsync(user_global.CNP);
            if (!trackOn)
            {
                // If turned off remotely, stop local tracking & UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StopTracking();
                    TrackButton.Text = "Start Tracking";
                });
                return; // do not fetch/save location
            }

            // 2) Proceed with location update
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
            // log/handle errors as needed
            Debug.WriteLine($"UpdateLocation error: {ex}");
        }
        finally
        {
            _tickLock.Release();
        }
    }
}
