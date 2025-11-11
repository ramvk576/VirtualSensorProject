using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using SensorEmulator.Core;

namespace SensorEmulator
{
    class Program
    {
        static void Main(string[] args)
        {
            DeleteUsbSensorsCfg();

            // Six sensors: odd = AccuTrac side, even = emulator listener.
            var profiles = new List<SensorProfile>
            {
                new SensorProfile("COM25","COM26","1235-1059526-007","SensorInput_COM25.csv", legacyCom25:true),
                new SensorProfile("COM27","COM28","1235-1059526-001","SensorInput_COM27.csv"),
                new SensorProfile("COM29","COM30","1235-1059526-002","SensorInput_COM29.csv"),
                new SensorProfile("COM31","COM32","1235-1059526-003","SensorInput_COM31.csv"),
                new SensorProfile("COM33","COM34","1235-1059526-004","SensorInput_COM33.csv"),
                new SensorProfile("COM35","COM36","1235-1059526-005","SensorInput_COM35.csv"),
                new SensorProfile("COM37","COM38","1716-1099531-001","UTS_Data.csv"),
                //new SensorProfile("COM39","COM40","1716-1099531-002","SensorInput_COM39.csv"),
                //new SensorProfile("COM41","COM42","1716-1099531-003","SensorInput_COM41.csv"),
                //new SensorProfile("COM43","COM44","1716-1099531-004","SensorInput_COM43.csv"),
                //new SensorProfile("COM45","COM46","1716-1099531-005","SensorInput_COM45.csv"),
                //new SensorProfile("COM47","COM48","1716-1099531-006","SensorInput_COM47.csv"),

            };

            foreach (var p in profiles)
            {
                // Ensure each CSV exists with a minimal dataset
                if (!File.Exists(p.CsvPath))
                {
                    File.WriteAllText(p.CsvPath, "VEL,TEMP\r\n0.396,28.55\r\n0.248,28.45\r\n0.335,28.53\r\n");
                }

                try
                {
                    var session = new SensorSession(p);
                    session.Start();   // spawns reader thread via SerialHandler
                    Logger.Log($"[{p.AccuPort}] Online via {p.ListenPort}. Serial {p.SerialNumber}. CSV {p.CsvPath}");
                    Thread.Sleep(40);  // small stagger for neat logs
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to start sensor on {p.ListenPort} for {p.AccuPort}: {ex.Message}");
                }
            }

            Logger.Log("All sensor listeners started. Ready to handle AccuTrac commands...");

            // Keep process alive
            while (true) Thread.Sleep(250);
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
