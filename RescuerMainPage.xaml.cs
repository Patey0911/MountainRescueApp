
using MountainRescueApp.Models;
using MountainRescueApp.Services;
using Microsoft.Maui.Controls.Shapes;  // Ellipse
using Firebase.Database.Streaming;     // FirebaseEvent<T>
using System.Collections.Generic;
using System.Linq;

namespace MountainRescueApp;

public partial class RescuerMainPage : ContentPage
{
    private IDisposable _usersSub;

    // Keep a quick lookup from Firebase key -> UI
    private readonly Dictionary<string, TouristUi> _cards = new();

    private readonly RescuerModel _rescuer;

    private sealed class TouristUi
    {
        public Frame Frame { get; init; }
        public Ellipse Dot { get; set; }   // created on demand
        public Label NameLabel { get; init; }
        public Label CnpLabel { get; init; }
        public UserModel Model { get; set; }
    }

    public RescuerMainPage(RescuerModel rescuer)
    {
        InitializeComponent();
        _rescuer = rescuer;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Start streaming from Firebase
        _usersSub = UserRepository.SubscribeToUsers(evt =>
        {
            if (evt == null) return;

            // Marshal UI changes to the UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (evt.EventType.ToString().Contains("Delete") || evt.Object == null)
                {
                    RemoveUserCard(evt.Key);
                }
                else
                {
                    // ⬇️ FILTER: doar userii de pe acelasi munte
                    if (string.Equals(evt.Object.Mountain, _rescuer.Mountain, StringComparison.OrdinalIgnoreCase))
                    {
                        UpsertUserCard(evt.Key, evt.Object);
                    }
                    else
                    {
                        // daca userul exista deja dar nu mai corespunde, il scoatem
                        RemoveUserCard(evt.Key);
                    }
                }
            });
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _usersSub?.Dispose();
        _usersSub = null;
    }


    private void UpsertUserCard(string key, UserModel user)
    {
        if (_cards.TryGetValue(key, out var existing))
        {
            // Update UI for existing card
            existing.Model = user;

            existing.NameLabel.Text = user.Name;
            existing.CnpLabel.Text = $"CNP: {user.CNP}";

            var userColor = Color.FromRgb(user.Red, user.Green, user.Blue);
            existing.Frame.BackgroundColor = userColor;

            SetTrackStatus(existing, user.Track);

            // ⬇️ ensure list is kept ordered after updates
            ResortUsersContainer();
            return;
        }

        // Create new card
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
            Model = user
        };
        _cards[key] = ui;

        UsersContainer.Children.Add(frame);

        SetTrackStatus(ui, user.Track);

        _ = frame.FadeTo(1, 250);

        // ⬇️ keep the container ordered after insert
        ResortUsersContainer();
    }

    private void RemoveUserCard(string key)
    {
        if (!_cards.TryGetValue(key, out var ui))
            return;

        _cards.Remove(key);

        _ = ui.Frame.FadeTo(0, 180).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UsersContainer.Children.Remove(ui.Frame);

                // ⬇️ re-order after removal (optional, keeps consistency)
                ResortUsersContainer();
            });
        });
    }

    // Optional: keep cards sorted by Name visually (case-insensitive, null-safe)
    private void ResortUsersContainer()
    {
        // Build ordered list of frames according to Name
        var ordered = _cards
            .OrderBy(kv =>
            {
                // null-safe, trim spaces
                var name = kv.Value.Model?.Name ?? string.Empty;
                return name.Trim();
            }, StringComparer.OrdinalIgnoreCase)
            .Select(kv => kv.Value.Frame)
            .ToList();

        // Re-render in order
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
                ui.Dot.Margin = new Thickness(0); // adjust inset if desired

                // place over the card content
                if (ui.Frame.Content is Grid grid)
                {
                    grid.Children.Add(ui.Dot);
                }
                StartFlicker(ui.Dot);
            }
            ui.Dot.IsVisible = true;
        }
        else
        {
            if (ui.Dot != null)
                ui.Dot.IsVisible = false; // keep it; reuse if Track turns back on
        }
    }

    /// <summary> Creates a small green circular dot. </summary>
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

    /// <summary> Flicker animation; keeps running while the dot stays in the visual tree. </summary>
    private void StartFlicker(VisualElement dot)
    {
        const int intervalMs = 500;
        bool dim = false;

        Device.StartTimer(TimeSpan.FromMilliseconds(intervalMs), () =>
        {
            if (dot?.Handler == null) return false; // stop if removed from UI

            var target = dim ? 1.0 : 0.3;
            dim = !dim;
            _ = dot.FadeTo(target, (uint)intervalMs, Easing.Linear);
            return true;
        });
    }
}
