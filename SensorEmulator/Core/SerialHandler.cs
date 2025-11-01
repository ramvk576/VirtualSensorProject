using System;
using System.IO.Ports;
using System.Threading;

namespace SensorEmulator.Core
{
    public class SerialHandler
    {
        private readonly SerialPort port;
        private readonly CommandProcessor processor;

        private string buffer = string.Empty;

        public SerialHandler(SerialPort serialPort, CommandProcessor cp)
        {
            port = serialPort;
            processor = cp;
        }

        public void Start()
        {
            new Thread(ReadLoop) { IsBackground = true }.Start();
        }

        private void ReadLoop()
        {
            Logger.Log($"Listening on {port.PortName}...");

            while (true)
            {
                try
                {
                    if (port.BytesToRead > 0)
                    {
                        string incoming = port.ReadExisting();
                        buffer += incoming;

                        Logger.Log("Received raw: " +
                            incoming.Replace("\r", "\\r").Replace("\n", "\\n"));

                        ProcessIncomingBuffer();
                    }

                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    Logger.Log("Serial read error: " + ex.Message);
                }
            }
        }

        private void ProcessIncomingBuffer()
        {
            // Process *V even without CR/LF terminator
            while (buffer.Contains("*V"))
            {
                int vIndex = buffer.IndexOf("*V", StringComparison.Ordinal);
                if (vIndex < 0) break;

                string response = processor.Process("*V");
                if (!string.IsNullOrEmpty(response))
                {
                    Thread.Sleep(30);
                    port.Write(response);
                    port.BaseStream.Flush();
                    Logger.Log("Sent: " + response.Replace("\r", "\\r").Replace("\n", "\\n"));
                }

                buffer = buffer.Remove(vIndex, 2); // Remove processed *V
            }

            // Handle CR/LF terminated commands
            while (buffer.Contains("\r") || buffer.Contains("\n"))
            {
                int idx = buffer.IndexOfAny(new char[] { '\r', '\n' });
                if (idx < 0) return;

                string line = buffer.Substring(0, idx).Trim();
                buffer = buffer.Substring(Math.Min(buffer.Length, idx + 1));

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Logger.Log("Parsed Command: " + line);

                string response = processor.Process(line);

                if (!string.IsNullOrEmpty(response))
                {
                    Thread.Sleep(30);
                    port.Write(response);
                    port.BaseStream.Flush();
                    Logger.Log("Sent: " + response.Replace("\r", "\\r").Replace("\n", "\\n"));
                }
            }
        }
    }
}
