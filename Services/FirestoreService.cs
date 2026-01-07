using System.Threading.Tasks;
using MountainRescueApp.Models;
using Google.Cloud.Firestore;

namespace MountainRescueApp.Services
{
    public class FirestoreService
    {
        private FirestoreDb db;
        private async Task SetupFirestore()
        {
            if (db == null)
            {
                var stream = await FileSystem.OpenAppPackageFileAsync("admin-sdk.json");
                var reader = new StreamReader(stream);
                var contents = reader.ReadToEnd();

                db = new FirestoreDbBuilder
                {
                    ProjectId = "mountainrescueappdb",

                    ConverterRegistry = new ConverterRegistry
                    {
                        new DateTimeToTimestampConverter(),
                    },
                    JsonCredentials = contents
                }.Build();
            }
        }
        public async Task InsertSampleModel(SampleModel sample)
        {
            await SetupFirestore();
            await db.Collection("SampleModels").AddAsync(sample);
        }

        public async Task InsertUser(UserModel user)
        {
            await SetupFirestore();
            await db.Collection("UserModel").AddAsync(user);
        }
        public async Task<List<SampleModel>> GetSampleModels()
        {
            await SetupFirestore();
            var data = await db
                            .Collection("SampleModels")
                            .GetSnapshotAsync();
            var sampleModels = data.Documents
                .Select(doc =>
                {
                    var sampleModel = doc.ConvertTo<SampleModel>();
                    sampleModel.Id = doc.Id; // FirebaseId hinzufügen
                    return sampleModel;
                })
                .ToList();
            return sampleModels;
        }

    }
}
