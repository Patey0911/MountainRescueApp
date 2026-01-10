using MountainRescueApp.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Timers;
using MountainRescueApp.Services;

namespace MountainRescueApp;

public partial class UserMainPage : ContentPage
{
    private System.Timers.Timer _timer;
    private bool _isTracking = false;
    private Polyline _userPath;
    LocationModel Userlocation = new LocationModel();
    UserModel user_global = new UserModel();
    UInt64 No_Location = 0;

    public UserMainPage(UserModel user)
    {
        InitializeComponent();

        // Create the polyline that will display the user's path
        _userPath = new Polyline
        {
            StrokeColor = Colors.Red,
            StrokeWidth = 15 // Thicker route line
        };

        user_global = user;

        // Add the polyline to the map
        mappy.MapElements.Add(_userPath);
    }

    protected override async void OnAppearing()

    { 
        base.OnAppearing();

        // Request location permission
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            return;

        // Get current location
        var location = await Geolocation.GetLocationAsync(
            new GeolocationRequest(GeolocationAccuracy.High)
        );

        if (location == null)
            return;

        // Center the map on the user's location
        mappy.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromKilometers(1)
            )
        );

        // Set satellite map mode
        mappy.MapType = MapType.Satellite;
    }

    private async void TrackButton_Clicked(object sender, EventArgs e)
    {
        if (_isTracking)
        {
            StopTracking();
            TrackButton.Text = "Start Tracking";
        }
        else
        {
            No_Location = 0;

            // Delete previous saved locations for this user
            await LocationRepository.Delete(user_global.CNP);

            // Clear the polyline path
            _userPath.Geopath.Clear();

            await StartTracking();
            TrackButton.Text = "Stop Tracking";
        }
    }

    private async Task StartTracking()
    {
        _isTracking = true;

        // Timer triggers every 5 seconds
        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += async (s, e) => await UpdateLocation();
        _timer.AutoReset = true;
        _timer.Enabled = true;

        // Get the first location immediately
        await UpdateLocation();
    }

    private async void StopTracking()
    {
        _isTracking = false;
        _timer?.Stop();
        _timer?.Dispose();
    }

    private async Task UpdateLocation()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.High));

            if (location != null)
            {
                var position = new Location(location.Latitude, location.Longitude);

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Move the map to the new location
                    mappy.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(50)));

                    // Add the new point to the polyline path
                    _userPath.Geopath.Add(position);

                    // Save the location to the database
                    Userlocation.LocationNo = No_Location;
                    Userlocation.Longitude = location.Longitude;
                    Userlocation.Latitude = location.Latitude;
                    Userlocation.CNP = user_global.CNP;

                    await LocationRepository.Save(Userlocation, user_global.CNP, No_Location);

                    No_Location = No_Location + 1;
                });
            }
        }
        catch (Exception ex)
        {
            // Handle GPS or permission errors
        }
    }
}
