using MountainRescueApp.Models;
using MountainRescueApp.Services;

namespace MountainRescueApp;

public partial class RescuerMainPage : ContentPage
{
    public RescuerMainPage(RescuerModel rescuer)
    {
        InitializeComponent();
        LoadTourists();
    }

    private async void LoadTourists()
    {
        var tourists = (await UserRepository.GetAllUsers())
                  .OrderBy(t => t.Name)
                  .ToList();

        foreach (var tourist in tourists)
        {
            // Build color from DB values
            var userColor = Color.FromRgb(tourist.Red, tourist.Green, tourist.Blue);

            var frame = new Frame
            {
                CornerRadius = 15,
                BackgroundColor = userColor,  
                Padding = 15,
                HasShadow = true,
                Opacity = 0,
                Content = new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                {
                    new Label
                    {
                        Text = tourist.Name,
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.Black
                    },
                    new Label
                    {
                        Text = $"CNP: {tourist.CNP}",
                        FontSize = 14,
                        TextColor = Colors.Gray
                    }
                }
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                await frame.ScaleTo(0.95, 100);
                await frame.ScaleTo(1.0, 100);

                await Navigation.PushAsync(new AllTouristsMapPage(tourist));
            };
            frame.GestureRecognizers.Add(tap);

            UsersContainer.Children.Add(frame);

            await frame.FadeTo(1, 250);
        }
    }
}
