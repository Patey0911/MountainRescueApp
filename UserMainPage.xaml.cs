using MountainRescueApp.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace MountainRescueApp;

public partial class UserMainPage : ContentPage
{
    public UserMainPage(UserModel user)
    {
        InitializeComponent();
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        // Cere permisiunea
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            return;

        // Obține locația curentă
        var location = await Geolocation.GetLocationAsync(
            new GeolocationRequest(GeolocationAccuracy.High)
        );

        if (location == null)
            return;

        // Centrează harta pe utilizator
        mappy.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromKilometers(1)
            )
        );

        // Setează modul satelit
        mappy.MapType = MapType.Satellite;

    }

}