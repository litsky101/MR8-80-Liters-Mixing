using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MR_8_80_Liters_Mixing_System
{
    public class Configuration
    {
        public static string GetConnString (string mainNode, string findNode)
        {
            string val = string.Empty;

            try
            {
                XDocument doc = XDocument.Load(Application.StartupPath + @"\Configuration.xml");

                var data = doc.Element("Configurations").Elements(mainNode)
                    .Select(n => new
                    {
                        conn = n.Element(findNode).Value
                    });

                           

                if (data != null)
                {
                    data.ToList().ForEach(b => val = b.conn.ToString());
                }
                else
                {
                    MessageDisplay.Error("Configuration file not found!\nPlease call IT immediately");
                    Environment.Exit(0);
                }

            }
            catch (NullReferenceException)
            {
                MessageDisplay.Error("Found null values in configuration settings");
                Environment.Exit(0);
            }

            return val;
        }

        public static string GetSettingData(string mainNode, string findNode)
        {
            string val = string.Empty;

            try
            {
                XDocument doc = XDocument.Load(Application.StartupPath + @"\Configuration.xml");

                var data = doc.Element("Configurations").Elements(mainNode)
                    .Select(n => new
                    {
                        conn = n.Element(findNode).Value
                    });



                if (data != null)
                {
                    data.ToList().ForEach(b => val = b.conn.ToString());
                }
                else
                {
                    MessageDisplay.Error("Configuration file not found!\nPlease call IT immediately");
                    Environment.Exit(0);
                }

            }
            catch (NullReferenceException)
            {
                MessageDisplay.Error("Found null values in configuration settings");
                Environment.Exit(0);
            }

            return val;
        }

        public static Dictionary<WeighingScales, string> GetSocket(string mainNode)
        {
            Dictionary<WeighingScales, string> dic = new Dictionary<WeighingScales, string>();

            try
            {
                XDocument doc = XDocument.Load(Application.StartupPath + @"\Configuration.xml");

                var data = doc.Element("Configurations").Elements(mainNode)
                    .Select(n =>
                    new
                    {
                        id = n.Element("ID").Value,
                        port = n.Element("Port").Value
                    });

                if (data != null)
                {
                    data.ToList().ForEach(n => dic.Add((WeighingScales)Enum.Parse(typeof(WeighingScales), n.id), n.port));
                }
                else
                {
                    MessageDisplay.Error("Configuration file not found!\nPlease call IT immediately");
                    Environment.Exit(0);
                }

                return dic;
            }
            catch(NullReferenceException)
            {
                MessageDisplay.Error("Found null values in configuration settings");
                Environment.Exit(0);
            }

            return dic;
        }

        public static bool IsRunning()
        {
            bool result = false;

            Process _process = Process.GetCurrentProcess();
            string procName = _process.ProcessName;

            if(Process.GetProcessesByName(procName).Length > 1)
            {
                result = true;
            }

            return result;
        }

    }

    public enum WeighingScales
    {
        MR8B1, MR8B2, MR8A, MR_RELEASING, KEMISORB, DYESTUFF, MRCAT, TANK, Stirring, MOXA
    }
}
