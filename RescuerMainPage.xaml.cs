using MountainRescueApp.Models;
using MountainRescueApp.Services;
using Microsoft.Maui.Controls.Shapes;
using Firebase.Database.Streaming;
using System.Collections.Generic;
using System.Linq;



namespace MountainRescueApp;

public partial class RescuerMainPage : ContentPage
{
    private IDisposable _usersSub;
    private IDisposable _emergenciesSub;

    private readonly Dictionary<string, TouristUi> _cards = new(); // KEY = CNP !!!
    private readonly Dictionary<string, bool> _lastEmergencyState = new(); // memorăm ultima stare

    private readonly RescuerModel _rescuer;

    private readonly Color EmergencyColor = Colors.Red;

    private sealed class TouristUi
    {
        public Frame Frame { get; init; }
        public Ellipse Dot { get; set; }
        public Label NameLabel { get; init; }
        public Label CnpLabel { get; init; }
        public UserModel Model { get; set; }
        public Color OriginalColor { get; set; }
    }

    public RescuerMainPage(RescuerModel rescuer)
    {
        InitializeComponent();
        _rescuer = rescuer;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _usersSub = UserRepository.SubscribeToUsers(evt =>
        {
            if (evt == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (evt.EventType.ToString().Contains("Delete") || evt.Object == null)
                {
                    RemoveUserCard(evt.Key);
                }
                else
                {
                    UpsertUserCard(evt.Object.CNP, evt.Object); // KEY = CNP !!!
                }
            });
        });

        StartEmergencyListener();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _usersSub?.Dispose();
        _usersSub = null;

        _emergenciesSub?.Dispose();
        _emergenciesSub = null;
    }

    private void UpsertUserCard(string cnp, UserModel user)
    {
        if (_cards.TryGetValue(cnp, out var existing))
        {
            existing.Model = user;
            existing.NameLabel.Text = user.Name;
            existing.CnpLabel.Text = $"CNP: {user.CNP}";

            var userColor = Color.FromRgb(user.Red, user.Green, user.Blue);
            existing.OriginalColor = userColor;

            if (existing.Frame.BackgroundColor != EmergencyColor)
                existing.Frame.BackgroundColor = userColor;

            SetTrackStatus(existing, user.Track);
            ResortUsersContainer();
            return;
        }

        var userColorNew = Color.FromRgb(user.Red, user.Green, user.Blue);

        var nameLabel = new Label
        {
            Text = user.Name,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black
        };

        var cnpLabel = new Label
        {
            Text = $"CNP: {user.CNP}",
            FontSize = 14,
            TextColor = Colors.DarkSlateGray
        };

        var contentLayout = new VerticalStackLayout
        {
            Spacing = 3,
            Children = { nameLabel, cnpLabel }
        };

        var container = new Grid { Padding = 0 };
        container.Children.Add(contentLayout);

        var frame = new Frame
        {
            CornerRadius = 15,
            BackgroundColor = userColorNew,
            Padding = 15,
            HasShadow = true,
            Opacity = 0,
            Content = container
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) =>
        {
            await frame.ScaleTo(0.95, 100);
            await frame.ScaleTo(1.0, 100);
            await Navigation.PushAsync(new AllTouristsMapPage(user));
        };
        frame.GestureRecognizers.Add(tap);

        var ui = new TouristUi
        {
            Frame = frame,
            NameLabel = nameLabel,
            CnpLabel = cnpLabel,
            Model = user,
            OriginalColor = userColorNew
        };

        _cards[cnp] = ui; // KEY = CNP !!!

        UsersContainer.Children.Add(frame);

        SetTrackStatus(ui, user.Track);

        _ = frame.FadeTo(1, 250);

        ResortUsersContainer();
    }

    private void RemoveUserCard(string cnp)
    {
        if (!_cards.TryGetValue(cnp, out var ui))
            return;

        _cards.Remove(cnp);

        _ = ui.Frame.FadeTo(0, 180).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UsersContainer.Children.Remove(ui.Frame);
                ResortUsersContainer();
            });
        });
    }

    private void ResortUsersContainer()
    {
        var ordered = _cards
            .OrderBy(kv => kv.Value.Model?.Name ?? "")
            .Select(kv => kv.Value.Frame)
            .ToList();

        UsersContainer.Children.Clear();
        foreach (var frame in ordered)
            UsersContainer.Children.Add(frame);
    }

    private void SetTrackStatus(TouristUi ui, bool onTrack)
    {
        if (onTrack)
        {
            if (ui.Dot == null)
            {
                ui.Dot = CreateFlickeringStatusDot();
                ui.Dot.HorizontalOptions = LayoutOptions.End;
                ui.Dot.VerticalOptions = LayoutOptions.Start;

                if (ui.Frame.Content is Grid grid)
                    grid.Children.Add(ui.Dot);

                StartFlicker(ui.Dot);
            }
            ui.Dot.IsVisible = true;
        }
        else
        {
            if (ui.Dot != null)
                ui.Dot.IsVisible = false;
        }
    }

    private static Ellipse CreateFlickeringStatusDot()
    {
        return new Ellipse
        {
            WidthRequest = 14,
            HeightRequest = 14,
            Fill = new SolidColorBrush(Colors.LimeGreen),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
            Opacity = 1
        };
    }

    private void StartFlicker(VisualElement dot)
    {
        const int intervalMs = 500;
        bool dim = false;

        Device.StartTimer(TimeSpan.FromMilliseconds(intervalMs), () =>
        {
            if (dot?.Handler == null) return false;

            var target = dim ? 1.0 : 0.3;
            dim = !dim;
            _ = dot.FadeTo(target, (uint)intervalMs, Easing.Linear);
            return true;
        });
    }

    // ---------------- EMERGENCIES REAL-TIME ----------------

    private void StartEmergencyListener()
    {
        _emergenciesSub = EmergenciesRepository.SubscribeToEmergencies(evt =>
        {
            if (evt?.Object == null || string.IsNullOrWhiteSpace(evt.Key))
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                HandleEmergencyEvent(evt.Key, evt.Object);
            });
        });
    }

    private void HandleEmergencyEvent(string cnp, EmergencyModel emergency)
    {
        if (!_cards.TryGetValue(cnp, out var ui))
            return;

        bool hasEmergency =
            emergency.Pierdut ||
            emergency.Entorsa ||
            emergency.Luxatie ||
            emergency.Fractura ||
            emergency.Contuzie ||
            emergency.Hipotermie ||
            emergency.Degeratura ||
            emergency.Insolatie ||
            emergency.Deshidratare ||
            emergency.RaudeAltitudine ||
            emergency.EpuizareFizica ||
            emergency.CrizaRespiratorie ||
            emergency.Avalansa ||
            emergency.Intepatura ||
            emergency.Muscatura;

        // Dacă nu avem stare anterioară, o salvăm
        if (!_lastEmergencyState.ContainsKey(cnp))
            _lastEmergencyState[cnp] = hasEmergency;

        // Dacă starea NU s-a schimbat, nu facem nimic
        if (_lastEmergencyState[cnp] == hasEmergency)
            return;

        // Actualizăm starea
        _lastEmergencyState[cnp] = hasEmergency;

        if (hasEmergency)
        {
            VibratePhone();
            StartAlarmLoop();
            StartCardFlicker(ui);
        }
        else
        {
            StopCardFlicker(ui);
        }
    }

    private async void StartAlarmLoop()
    {
        var audio = ServiceHelper.GetService<IAudioService>();
        audio.PlayAlertLoop();

        await DisplayAlert("Emergency Alert", "A tourist has reported an emergency!", "OK");

        audio.StopAlert();
    }


    private async void ShowEmergencyPopup(UserModel user)
    {
        await DisplayAlert("Emergency Alert", $"{user.Name} has reported an emergency!", "OK");
    }

    private void VibratePhone()
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
        }
        catch { }
    }

    private void StartCardFlicker(TouristUi ui)
    {
        var frame = ui.Frame;
        frame.BackgroundColor = EmergencyColor;

        const int intervalMs = 500;
        bool dim = false;

        Device.StartTimer(TimeSpan.FromMilliseconds(intervalMs), () =>
        {
            if (frame?.Handler == null) return false;

            var target = dim ? 1.0 : 0.4;
            dim = !dim;
            _ = frame.FadeTo(target, (uint)intervalMs, Easing.Linear);
            return true;
        });
    }

    private void StopCardFlicker(TouristUi ui)
    {
        var frame = ui.Frame;
        frame.BackgroundColor = ui.OriginalColor;
        frame.Opacity = 1.0;
    }
    private void PlayAlarmSound()
    {
        var audio = ServiceHelper.GetService<IAudioService>();
        StartAlarmLoop();
    }
}
