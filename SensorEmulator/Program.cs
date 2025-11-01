using System;
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
