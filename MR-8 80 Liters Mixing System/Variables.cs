using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MR_8_80_Liters_Mixing_System
{
    public class Variables
    {
        public static double mr8B1 { get; set; }
        public static double mr8B2 { get; set; }
        public static double mr8A { get; set; }
        public static double mrReleasing { get; set; }
        public static double kemisorb { get; set; }
        public static double dyeStuff { get; set; }
        public static double mrCatalyst { get; set; }


        public static string portMR8B1 { get; set; }
        public static string portMR8B2 { get; set; }
        public static string portMR8A { get; set; }
        public static string portMRReleasing { get; set; }
        public static string portKemisorb { get; set; }
        public static string portDyestuff { get; set; }
        public static string portMRCat { get; set; }
        public static string portTANK { get; set; }
        public static string ip { get; set; }
        public static string setupValue { get; set; }

        public static int mixerID { get; set; }
        public static int checkerID { get; set; }
        public static int returnID { get; set; }


        public static int minutes { get; set; }
        public static int seconds { get; set; }
        public static string weightVal { get; set; }

        public static string[] monomerType { get; set; }
        public static string batchNo { get; set; }

        public static string connString { get; set; }

        public class Machine
        {
            public static bool wait;
        }
    }
}
