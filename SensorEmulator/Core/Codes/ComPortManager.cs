using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace SensorEmulator.Core
{
    public class ComPair
    {
        public int SensorCom;   // odd, visible in AccuTrac
        public int ListenerCom; // even, emulator binds here
    }

    public sealed class ComPortManager
    {
        private readonly Config _cfg;
        private readonly string _exeDir;
        private readonly string _setupc;
        private readonly List<ComPair> _created = new List<ComPair>();
        private readonly string _portLogPath;

        public ComPortManager(Config cfg, string exeDir)
        {
            _cfg = cfg;
            _exeDir = exeDir;
            _setupc = cfg.SetupcPath;
            _portLogPath = Path.Combine(exeDir, "PortSetupLog.txt");
        }

        private void PortLog(string msg)
        {
            string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + msg;
            Console.WriteLine(line);
            File.AppendAllText(_portLogPath, line + Environment.NewLine, Encoding.UTF8);
        }

        public List<ComPair> CreateOrderedPairs()
        {
            var result = new List<ComPair>();

            if (!IsCom0ComAvailable())
            {
                PortLog("com0com not available. Skipping dynamic pair creation.");
                return result;
            }

            // Current COM usage
            HashSet<int> existing = new HashSet<int>();
            foreach (string n in SerialPort.GetPortNames())
            {
                if (n.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(n.Substring(3), out v) && v > 0) existing.Add(v);
                }
            }

            int nextOdd = (_cfg.StartCom % 2 == 1) ? _cfg.StartCom : _cfg.StartCom + 1;
            int pairsNeeded = _cfg.SensorPairs;
            int index = 1;

            while (pairsNeeded > 0 && nextOdd < 1024)
            {
                int sensorCom = nextOdd;
                int listenerCom = nextOdd + 1;

                if (!existing.Contains(sensorCom) && !existing.Contains(listenerCom))
                {
                    // Friendly names with index suffix as requested (01, 02, 03…)
                    string sensorFN = _cfg.FriendlyNameBase + " " + index.ToString("00") + " (COM" + sensorCom + ")";
                    string listenerFN = "Listener (COM" + listenerCom + ")";

                    PortLog("Creating pair COM" + sensorCom + " <-> COM" + listenerCom + " ...");

                    // Install both ends with friendly names
                    string args = "install " +
                                  "PortName=COM" + sensorCom + " FriendlyName=\"" + sensorFN + "\" " +
                                  "PortName=COM" + listenerCom + " FriendlyName=\"" + listenerFN + "\"";

                    int exit; string so; string se;
                    Run(_setupc, args, out exit, out so, out se);

                    if (exit == 0)
                    {
                        PortLog("Pair created successfully: COM" + sensorCom + " <-> COM" + listenerCom);
                        var pair = new ComPair { SensorCom = sensorCom, ListenerCom = listenerCom };
                        _created.Add(pair);
                        result.Add(pair);

                        existing.Add(sensorCom);
                        existing.Add(listenerCom);

                        pairsNeeded--;
                        index++;
                    }
                    else
                    {
                        PortLog("Failed to create pair COM" + sensorCom + "/COM" + listenerCom + ". Exit=" + exit + ". StdErr=" + se);
                    }
                }

                nextOdd += 2;
            }

            if (result.Count == 0)
            {
                PortLog("No pairs were created. Possibly all requested COM numbers are in use.");
            }

            return result;
        }

        public void CleanupCreatedPairsIfNeeded()
        {
            if (!_cfg.AutoDelete)
            {
                PortLog("auto_delete=false. Skipping removal of created ports.");
                return;
            }

            RemoveCreatedPairs();
        }

        public void RemoveCreatedPairs()
        {
            if (_created.Count == 0)
            {
                PortLog("No created pairs to remove.");
                return;
            }

            foreach (ComPair p in _created)
            {
                try
                {
                    PortLog("Removing pair COM" + p.SensorCom + "/COM" + p.ListenerCom + " ...");
                    string args = "remove PortName=COM" + p.SensorCom + " PortName=COM" + p.ListenerCom;
                    int exit; string so; string se;
                    Run(_setupc, args, out exit, out so, out se);

                    if (exit == 0) PortLog("Removed COM" + p.SensorCom + "/COM" + p.ListenerCom + ".");
                    else PortLog("Failed to remove COM" + p.SensorCom + "/COM" + p.ListenerCom + ". Exit=" + exit + ". StdErr=" + se);
                }
                catch (Exception ex)
                {
                    PortLog("Error removing COM" + p.SensorCom + "/COM" + p.ListenerCom + ": " + ex.Message);
                }
            }

            _created.Clear();
        }

        private bool IsCom0ComAvailable()
        {
            try
            {
                if (!File.Exists(_setupc))
                {
                    PortLog("setupc.exe not found at '" + _setupc + "'.");
                    return false;
                }

                int exit; string so; string se;
                Run(_setupc, "list", out exit, out so, out se);
                if (exit == 0)
                {
                    PortLog("com0com CLI detected.");
                    return true;
                }

                PortLog("com0com CLI did not respond correctly to 'list'.");
                return false;
            }
            catch (Exception ex)
            {
                PortLog("Error probing com0com: " + ex.Message);
                return false;
            }
        }

        private static void Run(string file, string args, out int exitCode, out string stdOut, out string stdErr)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = file;
            psi.Arguments = args;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            using (var p = Process.Start(psi))
            {
                stdOut = p.StandardOutput.ReadToEnd();
                stdErr = p.StandardError.ReadToEnd();
                p.WaitForExit();
                exitCode = p.ExitCode;
            }
        }
    }
}
