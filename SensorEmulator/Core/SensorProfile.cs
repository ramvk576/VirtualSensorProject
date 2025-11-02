using System;

namespace SensorEmulator.Core
{
    public sealed class SensorProfile
    {
        public string AccuPort { get; }
        public string ListenPort { get; }
        public string SerialNumber { get; }
        public string CsvPath { get; }
        public bool KeepLegacyCom25 { get; }

        public SensorProfile(string accuPort, string listenPort, string serialNumber, string csvPath, bool legacyCom25 = false)
        {
            AccuPort = accuPort;
            ListenPort = listenPort;
            SerialNumber = serialNumber;
            CsvPath = csvPath;
            KeepLegacyCom25 = legacyCom25;
        }

        public int SerialSuffix
        {
            get
            {
                int dash = SerialNumber.LastIndexOf('-');
                if (dash < 0 || dash + 1 >= SerialNumber.Length) return 0;
                if (int.TryParse(SerialNumber.Substring(dash + 1), out var v)) return v;
                return 0;
            }
        }
    }
}
