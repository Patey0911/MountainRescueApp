using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountainRescueApp.Models
{
    [FirestoreData]
    public class UserModel
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

        public UserModel() { }

        public UserModel(string name, string password, string email, string cnp, string no_tel)
        {
            Name = name;
            Password = password;
            Email = email;
            CNP = cnp;
            No_Tel = no_tel;
        }
    }
}
