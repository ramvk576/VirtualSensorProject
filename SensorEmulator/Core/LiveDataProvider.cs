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
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 2) continue;

                csvValues.Add(new SensorData(parts[0].Trim(), parts[1].Trim()));
            }
        }

        public SensorData Next()
        {
            var val = csvValues[index];
            index = (index + 1) % csvValues.Count;
            return val;
        }

        public bool HasData => csvValues.Count > 0;
    }
}
