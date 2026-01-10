using Firebase.Database;
using Firebase.Database.Query;
using MountainRescueApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountainRescueApp.Services
{
    internal class UserRepository
    {
        static FirebaseClient firebaseClient = new FirebaseClient("https://mountainrescueappdb-default-rtdb.firebaseio.com/");

        public static async Task Save(UserModel user, int User_Number)
        {
            await firebaseClient.Child("Users/User" + User_Number).PutAsync(user);
        }

        public static async Task<List<UserModel>> GetAllUsers()
        {
            var userlist = (await firebaseClient
            .Child("Users")
            .OnceAsync<UserModel>()).Select(item =>
            new UserModel
            {
                Name = item.Object.Name,
                Password = item.Object.Password,
                Email = item.Object.Email,
                CNP = item.Object.CNP,
                No_Tel = item.Object.No_Tel,
                Red = item.Object.Red,
                Green = item.Object.Green,
                Blue = item.Object.Blue
            }).ToList();
            return userlist;
        }

        public static async Task<UserModel> GetByEmail(string email)
        {
            try
            {
                var allUsers = await GetAllUsers();
                await firebaseClient
                .Child("Users")
                .OnceAsync<UserModel>();
                return allUsers.Where(a => a.Email == email).FirstOrDefault();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error:{e}");
                return null;
            }
        }
    }
}
