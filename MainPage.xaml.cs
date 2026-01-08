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
            txtPassword.Text = "";
            txtUsername.Text = "";
        }
        private void txtUsername_Completed(object sender, EventArgs e)
        {
            txtPassword.Focus();
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            if (txtPassword.IsPassword)
            {
                password_icon.Source = "showpasswordwhite.png";
                txtPassword.IsPassword = false;
            }
            else
            {
                password_icon.Source = "hidepasswordwhite.png";
                txtPassword.IsPassword = true;
            }
        }

        private async void Button_ClickedAsync(object sender, EventArgs e)
        {
            String email, password;
            bool found = false;
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
                        found = true;
                        await Navigation.PushAsync(new UserMainPage(user));
                        break;
                    case 3:
                        await DisplayAlert("Failed", "Login failed", "OK");
                        break;
                    default:
                        break;
                }
            }
            if (found == false)
            {
                rescuer = await RescuerRepository.GetByEmail(email);
                switch (LoginValidationRescuer(email, password, rescuer))
                {
                    case 0:
                        await DisplayAlert("Alert", "User with this email doesn't exist", "OK");
                        break;
                    case 2:
                        //await Navigation.PushAsync(new MainPage(user));
                        await DisplayAlert("Alert", "Merge", "OK");
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
    }
}
