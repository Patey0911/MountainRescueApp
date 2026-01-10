using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountainRescueApp.Models
{
    public class LocationModel
    {
        [FirestoreProperty]
        public double Latitude { get; set; }

        [FirestoreProperty]
        public double Longitude { get; set; }

        [FirestoreProperty]
        public String CNP { get; set; }

        [FirestoreProperty]
        public UInt64 LocationNo { get; set; }

        public LocationModel() { }
        public LocationModel(UInt64 locationo, double latitude, double longitude, String cnp)
        {
            LocationNo = locationo;
            Latitude = latitude;
            Longitude = longitude;
            CNP = cnp;
        }
    }
}
