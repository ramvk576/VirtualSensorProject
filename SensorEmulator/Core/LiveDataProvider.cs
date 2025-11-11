using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SensorEmulator.Core
{
    public class LiveDataProvider
    {
        private readonly List<SensorData> csvValues = new List<SensorData>();
        private int index = 0;

        public void Load(string path)
        {
            if (!File.Exists(path))
                return;

            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');

                // ✅ Handle both UAS (VEL,TEMP) and UTS (TEMP only)
                if (parts.Length == 1)
                {
                    // UTS: only temperature available, set dummy velocity
                    csvValues.Add(new SensorData("0.000", parts[0].Trim()));
                }
                else if (parts.Length >= 2)
                {
                    // UAS: both velocity and temperature
                    csvValues.Add(new SensorData(parts[0].Trim(), parts[1].Trim()));
                }
            }
        }

        public SensorData Next()
        {
            if (csvValues.Count == 0)
                return new SensorData("0.000", "0.000"); // safe default if CSV missing

            var val = csvValues[index];
            index = (index + 1) % csvValues.Count;
            return val;
        }

        public bool HasData => csvValues.Count > 0;
    }
}
