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
    internal class EmergenciesRepository
    {
        static FirebaseClient firebaseClient = new FirebaseClient("https://mountainrescueappdb-default-rtdb.firebaseio.com/");

        public static async Task Save(EmergenciesModel emergencie)
        {
            await firebaseClient.Child("Emergencies/" + emergencie.CNP).PutAsync(emergencie);
        }

        public static async Task<List<EmergenciesModel>> GetAllEmergencies()
        {
            var emergencielist = (await firebaseClient
            .Child("Emergencies")
            .OnceAsync<EmergenciesModel>()).Select(item =>
            new EmergenciesModel
            {

                CNP = item.Object.CNP,
                Pierdut = item.Object.Pierdut,
                Entorsa = item.Object.Entorsa,
                Luxatie = item.Object.Luxatie,
                Fractura = item.Object.Fractura,
                Contuzie = item.Object.Contuzie,
                Hipotermie = item.Object.Hipotermie,
                Degeratura = item.Object.Degeratura,
                Insolatie = item.Object.Insolatie,
                Deshidratare = item.Object.Deshidratare,
                RaudeAltitudine = item.Object.RaudeAltitudine,
                EpuizareFizica = item.Object.EpuizareFizica,
                CrizaRespiratorie = item.Object.CrizaRespiratorie,
                Avalansa = item.Object.Avalansa,
                Intepatura = item.Object.Intepatura,
                Muscatura = item.Object.Muscatura
            }).ToList();
            return emergencielist;
        }

        public static async Task<EmergenciesModel> GetByCNP(string cnp)
        {
            try
            {
                var allUsers = await GetAllEmergencies();
                await firebaseClient
                .Child("Emergencies")
                .OnceAsync<EmergenciesModel>();
                return allUsers.Where(a => a.CNP == cnp).FirstOrDefault();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error:{e}");
                return null;
            }
        }
    }
}
