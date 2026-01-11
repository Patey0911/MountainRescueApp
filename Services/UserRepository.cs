using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using MountainRescueApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
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
                Mountain = item.Object.Mountain,
                Red = item.Object.Red,
                Green = item.Object.Green,
                Blue = item.Object.Blue,
                Track = item.Object.Track
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

        public static async Task UpdateTrack(UserModel user, bool track)
        {

            try
            {
                // Pull all user snapshots (we need their Keys to patch the right node)
                var snapshots = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UserModel>();

                // Filter locally by CNP (string equality, same as GetByEmail)
                var matches = snapshots
                    .Where(s => s?.Object?.CNP == user.CNP)
                    .ToList();

                int updated = 0;
                foreach (var snap in matches)
                {
                    // Use the actual node key (e.g., "User1")
                    await firebaseClient
                        .Child("Users")
                        .Child(snap.Key)
                        .PatchAsync(new { Track = track });

                    updated++;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateTrackByCnpAsync error: {ex}");
            }

        }
        public static async Task<bool> GetTrackByCnpAsync(string cnp)
        {
            try
            {
                // Keep same pattern as your working GetByEmail:
                var snapshots = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UserModel>();

                var match = snapshots
                    .Select(s => s.Object)
                    .FirstOrDefault(u => u?.CNP == cnp);

                // If Track is bool:
                return match?.Track == true;

                // If Track is int (0/1), use:
                // return match != null && match.Track == 1;

                // If Track is string ("true"/"false"), use:
                // return match != null && string.Equals(match.Track, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetTrackByCnpAsync error: {ex}");
                return false;
            }
        }


        public static IDisposable SubscribeToUsers(
                    Action<FirebaseEvent<UserModel>> onEvent,
                    Action<Exception> onError = null)
        {
            return firebaseClient
            .Child("Users")
            .AsObservable<UserModel>()
                .Subscribe(onEvent, onError ?? (ex => System.Diagnostics.Debug.WriteLine(ex)));
        }

    }
}
