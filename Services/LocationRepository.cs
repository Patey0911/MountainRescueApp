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
    internal class LocationRepository
    {
        public static FirebaseClient firebaseClient = new FirebaseClient("https://mountainrescueappdb-default-rtdb.firebaseio.com/");

        public static async Task Save(LocationModel Location, string CNP, UInt64 LocationNumber)
        {
            await firebaseClient.Child("Locations/" + CNP + "/Location" + LocationNumber).PutAsync(Location);
        }
        public static async Task<List<LocationModel>> GetAllLocations()
        {
            var locationslist = (await firebaseClient
            .Child("Locations")
            .OnceAsync<LocationModel>()).Select(item =>
            new LocationModel
            {
                Longitude = item.Object.Longitude,
                Latitude = item.Object.Latitude,
                CNP = item.Object.CNP,
                LocationNo = item.Object.LocationNo
            }).ToList();
            return locationslist;
        }

        public static async Task<List<LocationModel>> GetByCNP(string CNP)
        {
            try
            {
                var firebaseData = await firebaseClient
                    .Child("Locations")
                    .Child(CNP)
                    .OnceAsync<LocationModel>();

                return firebaseData
                    .Select(item => item.Object)
                    .OrderBy(loc => loc.LocationNo) // ordonează traseul corect
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error: {e}");
                return new List<LocationModel>();
            }
        }



        public static async Task<LocationModel> Delete(string CNP)
        {
            await firebaseClient
                .Child("Locations")
                .Child(CNP)
                .DeleteAsync();
            return null;
        }
    }
}
