using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SensorEmulator.Core;

namespace SensorEmulator
{
    class Program
    {
        static void Main(string[] args)
        {
            DeleteUsbSensorsCfg();

            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            var profiles = new List<SensorProfile>
            {
                // UAS (6)
                new SensorProfile("COM25","COM26","1235-1059526-007",Path.Combine(basePath,"SensorInput_COM25.csv"), legacyCom25:true),
                new SensorProfile("COM27","COM28","1235-1059526-001",Path.Combine(basePath,"SensorInput_COM27.csv")),
                new SensorProfile("COM29","COM30","1235-1059526-002",Path.Combine(basePath,"SensorInput_COM29.csv")),
                new SensorProfile("COM31","COM32","1235-1059526-003",Path.Combine(basePath,"SensorInput_COM31.csv")),
                new SensorProfile("COM33","COM34","1235-1059526-004",Path.Combine(basePath,"SensorInput_COM33.csv")),
                new SensorProfile("COM35","COM36","1235-1059526-005",Path.Combine(basePath,"SensorInput_COM35.csv")),

                // UTS (6) – TEMP1 only, CSV has single TEMP column
                new SensorProfile("COM37","COM38","1716-1099531-001",Path.Combine(basePath,"SensorInput_COM37.csv")),
                new SensorProfile("COM39","COM40","1716-1099531-002",Path.Combine(basePath,"SensorInput_COM39.csv")),
                new SensorProfile("COM41","COM42","1716-1099531-003",Path.Combine(basePath,"SensorInput_COM41.csv")),
                new SensorProfile("COM43","COM44","1716-1099531-004",Path.Combine(basePath,"SensorInput_COM43.csv")),
                new SensorProfile("COM45","COM46","1716-1099531-005",Path.Combine(basePath,"SensorInput_COM45.csv")),
                new SensorProfile("COM47","COM48","1716-1099531-006",Path.Combine(basePath,"SensorInput_COM47.csv")),

                // UHS (6) – HUM + TEMP, CSV has 2 columns (HUM,TEMP or VEL,TEMP)
                new SensorProfile("COM49","COM50","1508-00000-001",Path.Combine(basePath,"SensorInput_COM49.csv")),
                new SensorProfile("COM51","COM52","1508-00000-002",Path.Combine(basePath,"SensorInput_COM51.csv")),
                new SensorProfile("COM53","COM54","1508-00000-003",Path.Combine(basePath,"SensorInput_COM53.csv")),
                new SensorProfile("COM55","COM56","1508-00000-004",Path.Combine(basePath,"SensorInput_COM55.csv")),
                new SensorProfile("COM57","COM58","1508-00000-005",Path.Combine(basePath,"SensorInput_COM57.csv")),
                new SensorProfile("COM59","COM60","1508-00000-006",Path.Combine(basePath,"SensorInput_COM59.csv")),
            };

            foreach (var p in profiles)
            {
                try
                {
                    EnsureCsvExists(p);

                    var session = new SensorSession(p);
                    session.Start();

                    Logger.Log($"[{p.AccuPort}] Online via {p.ListenPort}. Serial {p.SerialNumber}. CSV {p.CsvPath}");
                    Thread.Sleep(40); // small stagger
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to start sensor on {p.ListenPort} for {p.AccuPort}: {ex.Message}");
                }
            }

            Logger.Log("All sensor listeners started. Ready to handle AccuTrac commands...");
            while (true) Thread.Sleep(250);
        }

        private static void EnsureCsvExists(SensorProfile p)
        {
            if (File.Exists(p.CsvPath))
                return;

            Logger.Log($"CSV missing for {p.SerialNumber}: {p.CsvPath}. Creating default.");

            // UTS – TEMP only
            if (p.SerialNumber.StartsWith("1716") || p.SerialNumber.StartsWith("UTS", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(p.CsvPath,
                    "TEMP\r\n" +
                    "28.11\r\n" +
                    "28.22\r\n" +
                    "28.33\r\n");
            }
            // UHS / UAS – 2 columns
            else
            {
                File.WriteAllText(p.CsvPath,
                    "VEL,TEMP\r\n" +
                    "0.25,28.50\r\n" +
                    "0.30,28.60\r\n" +
                    "0.35,28.70\r\n");
            }
        }

        private static void DeleteUsbSensorsCfg()
        {
            try
            {
                string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "USBSensors.cfg");
                if (File.Exists(cfgPath))
                {
                    File.Delete(cfgPath);
                    Logger.Log("Deleted existing USBSensors.cfg");
                }
                else
                {
                    Logger.Log("USBSensors.cfg not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to delete USBSensors.cfg: " + ex.Message);
            }
        }
    }
}
