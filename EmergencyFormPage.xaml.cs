
using MountainRescueApp.Models;
using MountainRescueApp.Services;

namespace MountainRescueApp;

public partial class EmergencyFormPage : ContentPage
{
    private readonly UserModel _user;

    public EmergencyFormPage(UserModel user)
    {
        InitializeComponent();
        _user = user;
    }

    private async void Submit_Clicked(object sender, EventArgs e)
    {
        // Optional: ensure tracking still on
        var trackOn = await UserRepository.GetTrackByCnpAsync(_user.CNP);
        if (!trackOn)
        {
            await DisplayAlert("Tracking Off", "Tracking is off. Please enable tracking to report an emergency.", "OK");
            return;
        }

        bool accidentat =
            PierdutSwitch.IsToggled ||
            EntorsaSwitch.IsToggled ||
            LuxatieSwitch.IsToggled ||
            FracturaSwitch.IsToggled ||
            ContuzieSwitch.IsToggled ||
            HipotermieSwitch.IsToggled ||
            DegeraturaSwitch.IsToggled |
            InsolatieSwitch.IsToggled ||
            DeshidratareSwitch.IsToggled ||
            RaudeAltitudineSwitch.IsToggled ||
            EpuizareFizicaSwitch.IsToggled ||
            CrizaRespiratorieSwitch.IsToggled ||
            AvalansaSwitch.IsToggled ||
            IntepaturaSwitch.IsToggled||
            MuscaturaSwitch.IsToggled;

        var model = new EmergencyModel
        {
            CNP = _user.CNP,
            Pierdut = PierdutSwitch.IsToggled,
            Entorsa = EntorsaSwitch.IsToggled,
            Luxatie = LuxatieSwitch.IsToggled,
            Fractura = FracturaSwitch.IsToggled,
            Contuzie = ContuzieSwitch.IsToggled,
            Hipotermie = HipotermieSwitch.IsToggled,
            Degeratura = DegeraturaSwitch.IsToggled,
            Insolatie = InsolatieSwitch.IsToggled,
            Deshidratare = DeshidratareSwitch.IsToggled,
            RaudeAltitudine = RaudeAltitudineSwitch.IsToggled,
            EpuizareFizica = EpuizareFizicaSwitch.IsToggled,
            CrizaRespiratorie = CrizaRespiratorieSwitch.IsToggled,
            Avalansa = AvalansaSwitch.IsToggled,
            Intepatura = IntepaturaSwitch.IsToggled,
            Muscatura = MuscaturaSwitch.IsToggled,
            Accidentat = accidentat
        };

        try
        {
            // Save under "Emergencies/{CNP}" so rescuers can easily stream this
            await EmergenciesRepository.Save(model);

            await DisplayAlert("Sent", "Your emergency has been reported.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not send emergency: {ex.Message}", "OK");
        }
    }
}
