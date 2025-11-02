using System;
using System.Collections.Generic;
using System.IO;

namespace SensorEmulator.Core
{
    public sealed class Config
    {
        public bool AutoDelete = true;
        public int StartCom = 27;
        public int SensorPairs = 3; // you asked 3
        public string FriendlyNameBase = "DegreeC USB Sensor";
        public string SetupcPath = @"C:\Program Files (x86)\com0com\setupc.exe";

        public static Config Load(string exeDir)
        {
            var cfg = new Config();
            string path = Path.Combine(exeDir, "SensorEmulator.cfg");
            if (!File.Exists(path)) return cfg;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(path);
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("[")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq).Trim();
                string v = line.Substring(eq + 1).Trim();
                dict[k] = v;
            }

            string tmp;
            int n;
            if (dict.TryGetValue("auto_delete", out tmp)) cfg.AutoDelete = tmp.Equals("yes", StringComparison.OrdinalIgnoreCase) || tmp.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (dict.TryGetValue("min_port", out tmp) && int.TryParse(tmp, out n)) cfg.StartCom = n;
            if (dict.TryGetValue("sensor_count", out tmp) && int.TryParse(tmp, out n)) cfg.SensorPairs = Math.Max(1, n);
            if (dict.TryGetValue("friendly_name", out tmp)) cfg.FriendlyNameBase = tmp;
            if (dict.TryGetValue("setupc_path", out tmp) && !string.IsNullOrEmpty(tmp)) cfg.SetupcPath = tmp;

            return cfg;
        }
    }
}
