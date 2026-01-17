using MountainRescueApp.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Timers;
using MountainRescueApp.Services;
using System.Threading;
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
    private ulong No_Location = 0;

    private Location _lastPoint = null;
    private Location _lastCenter = null;

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

        EmergencyButton.IsEnabled = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            return;

        var location = await Geolocation.GetLocationAsync(
            new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1))
        );

        if (location == null)
            return;

        mappy.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromKilometers(1)
            )
        );

        mappy.MapType = MapType.Satellite;
    }

    private async void TrackButton_Clicked(object sender, EventArgs e)
    {
        if (_isTracking)
        {
            StopTracking();
            TrackButton.Text = "Start Tracking";
            EmergencyButton.IsEnabled = false;
            await UserRepository.UpdateTrack(user_global, false);
        }
        else
        {
            No_Location = 0;

            await LocationRepository.Delete(user_global.CNP);

            user_global.Track = true;
            await UserRepository.UpdateTrack(user_global, true);

            _userPath.Geopath.Clear();

            await StartTracking();

            TrackButton.Text = "Stop Tracking";
            EmergencyButton.IsEnabled = true;
        }
    }

    private async Task StartTracking()
    {
        _isTracking = true;

        _timer = new System.Timers.Timer(1000) // 1 second updates
        {
            AutoReset = true,
            Enabled = true
        };

        _timer.Elapsed += async (s, e) => await UpdateLocation();

        await UpdateLocation();
    }

    private void StopTracking()
    {
        _isTracking = false;
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private bool IsValidPoint(Location oldLoc, Location newLoc)
    {
        if (oldLoc == null)
            return true;

        // Calculate in KM, convert to meters
        double distKm = Location.CalculateDistance(oldLoc, newLoc, DistanceUnits.Kilometers);
        double distance = distKm * 1000;

        // Ignore jumps larger than 30 meters in 1 second
        return distance <= 30;
    }

    private async Task UpdateLocation()
    {
        if (!await _tickLock.WaitAsync(0))
            return;

        try
        {
            var trackOn = await UserRepository.GetTrackByCnpAsync(user_global.CNP);
            if (!trackOn)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StopTracking();
                    TrackButton.Text = "Start Tracking";
                    EmergencyButton.IsEnabled = false;
                });
                return;
            }

            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1))
            );

            if (location == null)
                return;

            var position = new Location(location.Latitude, location.Longitude);

            if (!IsValidPoint(_lastPoint, position))
                return;

            _lastPoint = position;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Smooth auto-centering
                if (_lastCenter == null)
                {
                    _lastCenter = position;
                }
                else
                {
                    double distKm = Location.CalculateDistance(_lastCenter, position, DistanceUnits.Kilometers);
                    double dist = distKm * 1000;

                    if (dist > 30)
                    {
                        mappy.MoveToRegion(
                            MapSpan.FromCenterAndRadius(position, Distance.FromMeters(80))
                        );
                        _lastCenter = position;
                    }
                }

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
        if (!_isTracking)
            return;

        await Navigation.PushAsync(new EmergencyFormPage(user_global));
    }
}
