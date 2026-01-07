using Google.Type;
using MountainRescueApp.Models;
using MountainRescueApp.Services;
using System.Text.RegularExpressions;

namespace MountainRescueApp;

public partial class UserRegisterPage : ContentPage
{
    public static string Name, Password, Email, CNP, Nr_Telefon;
    public UserRegisterPage()
    {
        InitializeComponent();
    }

    private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        if (txtPassword.IsPassword)
        {
            password_icon.Source = "showpassword.png";
            txtPassword.IsPassword = false;
        }
        else
        {
            password_icon.Source = "hidepassword.png";
            txtPassword.IsPassword = true;
        }
    }

    private void TapGestureRecognizer_Tapped_1(object sender, EventArgs e)
    {
        if (txtPassword_Confirm.IsPassword)
        {
            password_icon2.Source = "showpassword.png";
            txtPassword_Confirm.IsPassword = false;
        }
        else
        {
            password_icon2.Source = "hidepassword.png";
            txtPassword_Confirm.IsPassword = true;
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        //Get the input from the register form completed by the user
        Name = txtName.Text;
        Password = AESRepository.EncryptAesManaged(txtPassword.Text);
        Email = txtEmail.Text;
        CNP = txtCNP.Text;
        Nr_Telefon = txtNr_Telefon.Text;

        var user_list = await UserRepository.GetAllUsers();

        var user = new UserModel(Name, Password, Email, CNP, Nr_Telefon);
        //await UserRepository.Save(user);
        //Verify if the passwords match
        if (txtPassword.Text == txtPassword_Confirm.Text)
        {
            //Verify if the email is available
            if (await EmailVerification(Email))
            {
                //Verify if the email is in a correct form
                if (await EmailValidation(Email))
                {
                    if (await TelefonValidation(Nr_Telefon))
                    {
                        //Save the new user in Firebase

                        await UserRepository.Save(user, user_list.Count + 1);
                        await DisplayAlert("Succes", "The user has been added", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Warning!", "The phone number is not correct", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Warning!", "The email is not correct", "OK");
                }
            }
            else
            {
                await DisplayAlert("Warning!", "The email is already used", "OK");
            }
        }
        else
        {
            await DisplayAlert("Warning!", "Password doesn't match", "OK");
        }
    }

    public async Task<bool> EmailValidation(String Email)
    {
        var emailPattern = "^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$";

        if (!String.IsNullOrWhiteSpace(Email) && !(Regex.IsMatch(Email, emailPattern)))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public async Task<bool> TelefonValidation(String phoneNumber)
    {
       Regex PhoneRegex = new Regex(@"^(07\d{8}|\+407\d{8}|00407\d{8})$");

        if(string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        phoneNumber = phoneNumber.Replace(" ", "");

        return PhoneRegex.IsMatch(phoneNumber);
    }

    private async Task<bool> EmailVerification(String email)
    {
        //Get all the users and then check that the email is not already used
        var user_list = await UserRepository.GetAllUsers();

        foreach (var user in user_list)
        {
            if (user.Email == email)
            {
                return false;
            }
        }

        var rescuer_list = await RescuerRepository.GetAllRescuers();

        foreach (var rescuer in rescuer_list)
        {
            if (rescuer.Email == email)
            {
                return false;
            }
        }

        return true;
    }

    private void txtEmail_Completed(object sender, EventArgs e)
    {
        txtPassword.Focus();
    }

    private void txtName_Completed(object sender, EventArgs e)
    {
        txtNr_Telefon.Focus();
    }

    private void txtCNP_Completed(object sender, EventArgs e)
    {
        txtName.Focus();
    }

    private void txtPassword_Completed(object sender, EventArgs e)
    {
        txtPassword_Confirm.Focus();
    }
    private void txtNr_Telefon_Completed(object sender, EventArgs e)
    {
        txtEmail.Focus();
    }
}