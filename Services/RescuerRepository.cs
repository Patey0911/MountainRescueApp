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
    internal class RescuerRepository
    {
        static FirebaseClient firebaseClient = new FirebaseClient("https://mountainrescueappdb-default-rtdb.firebaseio.com/");

        public static async Task Save(RescuerModel rescuer, int Rescuer_Number)
        {
            await firebaseClient.Child("Rescuers/Rescuer" + Rescuer_Number).PutAsync(rescuer);
        }

        public static async Task<List<RescuerModel>> GetAllRescuers()
        {
            var rescuerlist = (await firebaseClient
            .Child("Rescuers")
            .OnceAsync<RescuerModel>()).Select(item =>
            new RescuerModel
            {
                Name = item.Object.Name,
                Password = item.Object.Password,
                Email = item.Object.Email,
                CNP = item.Object.CNP,
                No_Tel = item.Object.No_Tel
            }).ToList();
            return rescuerlist;
        }

        public static async Task<RescuerModel> GetByEmail(string email)
        {
            try
            {
                var allRescuers = await GetAllRescuers();
                await firebaseClient
                .Child("Rescuers")
                .OnceAsync<RescuerModel>();
                return allRescuers.Where(a => a.Email == email).FirstOrDefault();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error:{e}");
                return null;
            }
        }
    }
}
