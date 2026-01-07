using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountainRescueApp.Models
{
    internal class RescuerModel
    {
        [FirestoreProperty]
        public string Name { get; set; }
        [FirestoreProperty]
        public string Password { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string CNP { get; set; }

        [FirestoreProperty]
        public string No_Tel { get; set; }

        [FirestoreProperty]
        public string Identification_Id { get; set; }

        public RescuerModel() { }

        public RescuerModel(string name, string password, string email, string cnp, string no_tel, string identification_id)
        {
            Name = name;
            Password = password;
            Email = email;
            CNP = cnp;
            No_Tel = no_tel;
            Identification_Id = identification_id;
        }
    }
}
