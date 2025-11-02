using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using SensorEmulator.Core;

namespace SensorEmulator
{
    class Program
    {
        private static LiveDataProvider dataProvider;               // kept for backward compatibility (single instance)
        private static CommandProcessor commandProcessor;

        // Keep handles for multi-sensor run
        private static readonly List<SerialPort> _openPorts = new List<SerialPort>();
        private static readonly List<SerialHandler> _handlers = new List<SerialHandler>();
        private static readonly List<string> _tempCsvFiles = new List<string>();

        static void Main(string[] args)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;

            // Load INI-style config from SensorEmulator.cfg (same folder as EXE)
            Config cfg = Config.Load(exeDir);

            // Virtual COM management (create/remove) with com0com
            ComPortManager comMgr = new ComPortManager(cfg, exeDir);

            // Delete legacy USBSensors.cfg if present
            DeleteUsbSensorsCfg();

            bool preloadMode = args.Length > 0 && args[0].Equals("--preload", StringComparison.OrdinalIgnoreCase);

            // 1) Create ordered pairs starting at cfg.StartCom (default 27), count = cfg.SensorPairs (you set 3)
            List<ComPair> pairs = comMgr.CreateOrderedPairs();

            // If com0com not available or nothing created, fall back to your original single-port behavior
            if (pairs.Count == 0)
            {
                string portName = "COM26";
                try
                {
                    SerialPort singlePort = OpenAndInitPort(portName);
                    _openPorts.Add(singlePort);

                    // Use original CSV for single run
                    dataProvider = new LiveDataProvider();
                    dataProvider.Load("SensorInput.csv");
                    Logger.Log("CSV file loaded.");

                    commandProcessor = new CommandProcessor(dataProvider);

                    SerialHandler handler = new SerialHandler(singlePort, commandProcessor);
                    handler.Start();
                    _handlers.Add(handler);

                    Logger.Log("Listening on " + portName + "...");
                    if (preloadMode) SendPreloadResponses(singlePort);
                    Logger.Log("Ready to handle AccuTrac commands...");

                    // Keep process alive
                    for (; ; )
                        Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Logger.Log("Failed to open fallback port: " + ex.Message);
                }
                finally
                {
                    // Cleanup single port
                    try
                    {
                        foreach (var sp in _openPorts)
                        {
                            if (sp.IsOpen) sp.Close();
                        }
                    }
                    catch { /* ignore */ }

                    // Remove any created pairs if auto_delete=true (in this branch none were created)
                    comMgr.CleanupCreatedPairsIfNeeded();
                }

                return;
            }

            // 2) Multi-sensor mode: build per-sensor CSV with round-robin offsets, bind to even/listener COMs
            try
            {
                Logger.Log("Multi-sensor mode enabled. Pairs created: " + pairs.Count);

                // Prepare round-robin CSV slices
                string baseCsvPath = Path.Combine(exeDir, "SensorInput.csv");
                if (!File.Exists(baseCsvPath))
                {
                    Logger.Log("SensorInput.csv not found in " + exeDir + ". Creating a minimal placeholder.");
                    File.WriteAllText(baseCsvPath,
                        "VEL,TEMP\r\n0.396,28.55\r\n0.248,28.45\r\n0.335,28.53\r\n",
                        Encoding.UTF8);
                }

                // Load entire CSV once in memory
                List<string> csvRows = LoadCsvDataRows(baseCsvPath); // excludes header

                int totalSensors = pairs.Count;
                for (int i = 0; i < totalSensors; i++)
                {
                    // Build a temp CSV file for this sensor using rows i, i+totalSensors, i+2*totalSensors, ...
                    string tempCsv = Path.Combine(exeDir, string.Format("SensorInput_sensor{0:00}.csv", i + 1));
                    _tempCsvFiles.Add(tempCsv);

                    WriteRoundRobinCsv(tempCsv, csvRows, totalSensors, i);
                }

                // Open a port + handler per pair
                for (int i = 0; i < pairs.Count; i++)
                {
                    ComPair p = pairs[i];
                    string listenerCom = "COM" + p.ListenerCom; // even side for emulator binding
                    string thisTempCsv = _tempCsvFiles[i];

                    Logger.Log(string.Format("Sensor{0:00}: binding to {1} (sensor side COM{2}) using CSV slice {3}",
                        i + 1, listenerCom, p.SensorCom, Path.GetFileName(thisTempCsv)));

                    SerialPort sp = OpenAndInitPort(listenerCom);
                    _openPorts.Add(sp);

                    // Create an isolated provider per sensor instance fed by its temp CSV
                    LiveDataProvider provider = new LiveDataProvider();
                    provider.Load(thisTempCsv);

                    CommandProcessor proc = new CommandProcessor(provider);
                    SerialHandler h = new SerialHandler(sp, proc);
                    h.Start();

                    _handlers.Add(h);
                }

                Logger.Log("All sensor listeners opened. Ready to handle AccuTrac commands...");
                if (preloadMode)
                {
                    // Preload on first listener only
                    SendPreloadResponses(_openPorts[0]);
                    Logger.Log("All preload responses sent on first listener.");
                }

                // Keep the application alive
                for (; ; )
                    Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                Logger.Log("Startup error: " + ex.Message);
            }
            finally
            {
                // Graceful shutdown: close all serial ports
                try
                {
                    foreach (var sp in _openPorts)
                    {
                        if (sp != null && sp.IsOpen) sp.Close();
                    }
                }
                catch { /* ignore */ }

                // Cleanup temp CSV slices
                foreach (var f in _tempCsvFiles)
                {
                    try { if (File.Exists(f)) File.Delete(f); } catch { /* ignore */ }
                }

                // Remove only pairs created by this run if auto_delete=true
                comMgr.CleanupCreatedPairsIfNeeded();
            }
        }

        private static SerialPort OpenAndInitPort(string portName)
        {
            SerialPort serialPort = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One);
            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            serialPort.NewLine = "\r\n";
            serialPort.Handshake = Handshake.None;
            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;

            serialPort.Open();
            Logger.Log("Opened " + portName);
            return serialPort;
        }

        private static void DeleteUsbSensorsCfg()
        {
            string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "USBSensors.cfg");

            try
            {
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

        private static void SendPreloadResponses(SerialPort port)
        {
            string[] preload =
            {
                "*R1,OK",
                "*R12,OK",
                "P#1=0x00000007",
                "P#4=0x35333231",
                "P#5=0x3530312D",
                "P#6=0x36323539",
                "P#7=0x3730302D",
                "P#8=0x00000000",
                "P#9=0x00000000",
                "P#10=0x00000000",
                "P#11=0x00000000",
                "P#12=0x31534155",
                "P#13=0x2D303031",
                "P#14=0x54676E45",
                "P#15=0x00747365",
                "P#16=0x00000000",
                "P#17=0x00000000",
                "P#18=0x00000000",
                "P#19=0x00000000"
            };

            Logger.Log("Sending preload responses...");
            foreach (string msg in preload)
            {
                try
                {
                    port.Write(msg + "\r\n");
                    Logger.Log("Preload Sent: " + msg);
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending preload message: " + ex.Message);
                }
            }
        }

        // Load only data rows (skip header), trimmed, ignore empty
        private static List<string> LoadCsvDataRows(string csvPath)
        {
            var rows = new List<string>();
            string[] lines = File.ReadAllLines(csvPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (i == 0 && line.StartsWith("VEL", StringComparison.OrdinalIgnoreCase)) continue; // header
                rows.Add(line);
            }
            return rows;
        }

        // Create a CSV file with header and every Nth row selected starting at offset
        private static void WriteRoundRobinCsv(string outPath, List<string> allRows, int totalSensors, int offset)
        {
            using (var sw = new StreamWriter(outPath, false, Encoding.UTF8))
            {
                sw.WriteLine("VEL,TEMP"); // header as expected by your LiveDataProvider
                for (int idx = offset; idx < allRows.Count; idx += totalSensors)
                {
                    sw.WriteLine(allRows[idx]);
                }
            }
        }
    }
}



/*using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using SensorEmulator.Core;

namespace SensorEmulator
{
    class Program
    {
        private static readonly string logFile = "SensorLog.txt";
        private static readonly object logLock = new object();
        private static LiveDataProvider dataProvider;
        private static CommandProcessor commandProcessor;

        static void Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM26";
            bool preloadMode = args.Length > 1 && args[1].Equals("--preload", StringComparison.OrdinalIgnoreCase);

            DeleteUsbSensorsCfg();

            Console.WriteLine($"Starting Sensor Emulator on {portName} {(preloadMode ? "[PRELOAD MODE]" : "")}");

            if (File.Exists(logFile))
                File.Delete(logFile);

            SerialPort serialPort = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                NewLine = "\r\n",
                Handshake = Handshake.None,
                DtrEnable = true,
                RtsEnable = true
            };

            try
            {
                serialPort.Open();

                dataProvider = new LiveDataProvider();
                dataProvider.Load("SensorInput.csv");
                Logger.Log("CSV file loaded.");

                commandProcessor = new CommandProcessor(dataProvider);

                SerialHandler handler = new SerialHandler(serialPort, commandProcessor);
                handler.Start();

                Logger.Log($"Listening on {portName} (paired with COM25).");

                if (preloadMode)
                {
                    SendPreloadResponses(serialPort);
                    Logger.Log("All preload responses sent.");
                }

                Logger.Log("Ready to handle AccuTrac commands...");

                // Keep the application alive
                while (true)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to open port: " + ex.Message);
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    Logger.Log("Serial port closed.");
                }
            }
        }

        private static void DeleteUsbSensorsCfg()
        {
            string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "USBSensors.cfg");

            try
            {
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

        private static void SendPreloadResponses(SerialPort port)
        {
            string[] preload =
            {
                "*R1,OK",
                "*R12,OK",
                "P#1=0x00000007",
                "P#4=0x35333231",
                "P#5=0x3530312D",
                "P#6=0x36323539",
                "P#7=0x3730302D",
                "P#8=0x00000000",
                "P#9=0x00000000",
                "P#10=0x00000000",
                "P#11=0x00000000",
                "P#12=0x31534155",
                "P#13=0x2D303031",
                "P#14=0x54676E45",
                "P#15=0x00747365",
                "P#16=0x00000000",
                "P#17=0x00000000",
                "P#18=0x00000000",
                "P#19=0x00000000"
            };

            Logger.Log("Sending preload responses...");
            foreach (var msg in preload)
            {
                try
                {
                    port.Write(msg + "\r\n");
                    Logger.Log("Preload Sent: " + msg);
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending preload message: " + ex.Message);
                }
            }
        }

    }
}
*/
