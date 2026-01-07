using MountainRescueApp.Models;
using MountainRescueApp.Services;

namespace MountainRescueApp
{
    public partial class MainPage : ContentPage
    {
        FirestoreService firestoreService;
        public static UserModel user;
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
            if (string.IsNullOrEmpty(txtUsername.ToString()) || string.IsNullOrEmpty(txtPassword.ToString()))
                await DisplayAlert("Empty Values", "Please enter Email and Password", "OK");

            //Get the username and password from login page
            email = txtUsername.Text;
            password = txtPassword.Text;

            //Searching the user with that email
            user = await UserRepository.GetByEmail(email);


            switch (LoginValidation(email, password, user))
            {
                case 0:
                    await DisplayAlert("Alert", "User with this email doesn't exist", "OK");
                    break;
                case 1:
                    //await Navigation.PushAsync(new MainPage(user));
                    await DisplayAlert("Alert", "Merge", "OK");
                    break;
                case 2:
                    await DisplayAlert("Failed", "Login failed", "OK");
                    break;
                default:
                    break;
            }
        }

        private int LoginValidation(string email, string password, UserModel user)
        {
            //Verify the credentials from the user
            //Searching the user with that email
            if (user == null)
                return 0;

            //Decrypt the password from firebase
            user.Password = AESRepository.DecryptAesManaged(user.Password);
            //Verify if the passwords match
            if (user.Password != password)
                return 2;
            else
                return 1;
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
