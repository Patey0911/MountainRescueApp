using MountainRescueApp.Models;
using MountainRescueApp.Services;

namespace MountainRescueApp
{
    public partial class MainPage : ContentPage
    {
        FirestoreService firestoreService;
        public static UserModel user;
        public static RescuerModel rescuer;
        public MainPage()
        {
            InitializeComponent();
            firestoreService = new FirestoreService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Fade in the whole container
            await MainContainer.FadeTo(1, 150, Easing.CubicInOut);
            await MainContainer.TranslateTo(0, 0, 150, Easing.CubicOut);

            // Animate children
            foreach (var child in MainContainer.Children)
            {
                if (child is VisualElement ve)
                {
                    await ve.FadeTo(1, 150, Easing.CubicInOut);
                    await ve.TranslateTo(0, 0, 150, Easing.CubicOut);
                }
            }

            txtPassword.Text = "";
            txtUsername.Text = "";
        }
        private void txtUsername_Completed(object sender, EventArgs e)
        {
            txtPassword.Focus();
        }

        private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            // Tap animation
            await password_icon.ScaleTo(0.8, 100);
            await password_icon.ScaleTo(1, 100);

            // Toggle password visibility
            txtPassword.IsPassword = !txtPassword.IsPassword;
            password_icon.Source = txtPassword.IsPassword
                ? "HidePasswordWhite.png"
                : "ShowPasswordWhite.png";

        }

        private async void AnimateButton(Button button)
        {
            await button.ScaleTo(0.92, 80, Easing.CubicOut);   // press down
            await button.ScaleTo(1.0, 80, Easing.CubicIn);     // release
        }


        private async void Button_ClickedAsync(object sender, EventArgs e)
        {
            AnimateButton((Button)sender);
            String email, password;
            if (string.IsNullOrEmpty(txtUsername.ToString()) || string.IsNullOrEmpty(txtPassword.ToString()))
                await DisplayAlert("Empty Values", "Please enter Email and Password", "OK");

            //Get the username and password from login page
            email = txtUsername.Text;
            password = txtPassword.Text;

            //Searching the user with that email
            user = await UserRepository.GetByEmail(email);
            if (user != null)
            {
                switch (LoginValidationUser(email, password, user))
                {
                    case 0:
                        await DisplayAlert("Alert", "User with this email doesn't exist", "OK");
                        break;
                    case 2:
                        await Navigation.PushAsync(new UserMainPage(user));
                        break;
                    case 3:
                        await DisplayAlert("Failed", "Login failed", "OK");
                        break;
                    default:
                        break;
                }
            }
            if (user == null)
            {
                rescuer = await RescuerRepository.GetByEmail(email);
                switch (LoginValidationRescuer(email, password, rescuer))
                {
                    case 0:
                        await DisplayAlert("Alert", "User with this email doesn't exist", "OK");
                        break;
                    case 2:
                        await Navigation.PushAsync(new RescuerMainPage(rescuer));
                        break;
                    case 3:
                        await DisplayAlert("Failed", "Login failed", "OK");
                        break;
                    default:
                        break;
                }
            }
        }

        private int LoginValidationUser(string email, string password, UserModel user)
        {
            //Verify the credentials from the user
            //Searching the user with that email
            if (user == null)
                return 0;

            //Decrypt the password from firebase
            user.Password = AESRepository.DecryptAesManaged(user.Password);
            //Verify if the passwords match
            if (user.Password != password)
                return 3;
            else
                return 2;
        }

        private int LoginValidationRescuer(string email, string password, RescuerModel rescuer)
        {
            //Verify the credentials from the user
            //Searching the user with that email
            if (rescuer == null)
                return 0;

            //Decrypt the password from firebase
            rescuer.Password = AESRepository.DecryptAesManaged(rescuer.Password);
            //Verify if the passwords match
            if (rescuer.Password != password)
                return 3;
            else
                return 2;
        }
        private void TapGestureRecognizer_Tapped_1(object sender, EventArgs e)
        {
            Navigation.PushAsync(new UserRegisterPage());
        }
        private void TapGestureRecognizer_Tapped_2(object sender, EventArgs e)
        {
            Navigation.PushAsync(new RescueRegisterPage());
        }

        private void txtUsername_Completed_1(object sender, EventArgs e)
        {
            txtPassword.Focus();
        }
    }
}
