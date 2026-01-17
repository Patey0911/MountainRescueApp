using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountainRescueApp.Models
{
    [FirestoreData]
    public class EmergencyModel
    {
        [FirestoreProperty]
        public string CNP { get; set; }

        [FirestoreProperty]
        public bool Pierdut {  get; set; }

        [FirestoreProperty]
        public bool Entorsa { get; set; }

        [FirestoreProperty]
        public bool Luxatie { get; set; }

        [FirestoreProperty]
        public bool Fractura { get; set; }

        [FirestoreProperty]
        public bool Contuzie { get; set; }

        [FirestoreProperty]
        public bool Hipotermie { get; set; }

        [FirestoreProperty]
        public bool Degeratura { get; set; }

        [FirestoreProperty]
        public bool Insolatie { get; set; }

        [FirestoreProperty]
        public bool Deshidratare { get; set; }

        [FirestoreProperty]
        public bool RaudeAltitudine { get; set; }

        [FirestoreProperty]
        public bool EpuizareFizica { get; set; }

        [FirestoreProperty]
        public bool CrizaRespiratorie { get; set; }

        [FirestoreProperty]
        public bool Avalansa { get; set; }

        [FirestoreProperty]
        public bool Intepatura { get; set; }

        [FirestoreProperty]
        public bool Muscatura { get; set; }

        public EmergencyModel() { }


        public EmergencyModel(
            string cnp,
            bool pierdut,
            bool entorsa,
            bool luxatie,
            bool fractura,
            bool contuzie,
            bool hipotermie,
            bool degeratura,
            bool insolatie,
            bool deshidratare,
            bool raudeAltitudine,
            bool epuizareFizica,
            bool crizaRespiratorie,
            bool avalansa,
            bool intepatura,
            bool muscatura)
        {
            CNP = cnp;
            Pierdut = pierdut;
            Entorsa = entorsa;
            Luxatie = luxatie;
            Fractura = fractura;
            Contuzie = contuzie;
            Hipotermie = hipotermie;
            Degeratura = degeratura;
            Insolatie = insolatie;
            Deshidratare = deshidratare;
            RaudeAltitudine = raudeAltitudine;
            EpuizareFizica = epuizareFizica;
            CrizaRespiratorie = crizaRespiratorie;
            Avalansa = avalansa;
            Intepatura = intepatura;
            Muscatura = muscatura;
        }

    }
}
