using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace SensorEmulator.Core
{
    public class CommandProcessor
    {
        private readonly LiveDataProvider dataProvider;
        private readonly Dictionary<string, string> regMap;
        private readonly string serial;
        private readonly bool isUts;
        private readonly bool isUhs;

        public CommandProcessor(LiveDataProvider provider, Dictionary<string, string> registers, string serialNumber)
        {
            dataProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            regMap = registers ?? BuildDefaultRegisters();
            serial = serialNumber ?? string.Empty;

            isUts = serial.StartsWith("UTS", StringComparison.OrdinalIgnoreCase)
                    || serial.StartsWith("1716", StringComparison.OrdinalIgnoreCase);

            isUhs = serial.StartsWith("UHS", StringComparison.OrdinalIgnoreCase)
                    || serial.StartsWith("1508", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> BuildDefaultRegisters()
        {
            // Default UAS map
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["P#1"] = "0x00000007",
                ["P#4"] = "0x35333231",
                ["P#5"] = "0x3530312D",
                ["P#6"] = "0x36323539",
                ["P#7"] = "0x3730302D",
                ["P#8"] = "0x00000000",
                ["P#9"] = "0x00000000",
                ["P#10"] = "0x00000000",
                ["P#11"] = "0x00000000",
                ["P#12"] = "0x31534155",
                ["P#13"] = "0x2D303031",
                ["P#14"] = "0x54676E45",
                ["P#15"] = "0x00747365",
                ["P#30"] = "0x07E40515",
                ["P#31"] = "0x000001F4",
                ["P#32"] = "0x000001F4",
                ["P#33"] = "0x00000000",
                ["P#34"] = "0x00000000",
                ["P#145"] = "0x00030040"
            };
        }

        public string Process(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string command = raw.Trim();

            if (command.StartsWith("*RPage->:", StringComparison.Ordinal))
                command = "*R" + command.Substring(10);

            // ======================================
            // ✅ UTS SENSOR HANDLING (TYPE 3)
            // ======================================
            if (isUts)
            {
                if (command.Equals("*R1", StringComparison.OrdinalIgnoreCase))
                    return "P#1=0x00000003";

                if (command.Equals("*R12", StringComparison.OrdinalIgnoreCase)) return "P#12=0x31535455";
                if (command.Equals("*R13", StringComparison.OrdinalIgnoreCase)) return "P#13=0x54303030";
                if (command.Equals("*R14", StringComparison.OrdinalIgnoreCase)) return "P#14=0x3130302D";
                if (command.Equals("*R15", StringComparison.OrdinalIgnoreCase)) return "P#15=0x00000000";

                if (command.Equals("*V", StringComparison.OrdinalIgnoreCase))
                {
                    if (dataProvider.HasData)
                    {
                        var d = dataProvider.Next();
                        try
                        {
                            return ResponseBuilder.BuildT(d.TEMP);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("BuildT failed: " + ex.Message);
                            return "*V<SD>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                        }
                    }
                    return "*V<SD>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                }
            }

            // ======================================
            // ✅ UHS SENSOR HANDLING (TYPE 4)
            // ======================================
            if (isUhs)
            {
                if (command.Equals("*R1", StringComparison.OrdinalIgnoreCase))
                    return "P#1=0x00000004"; // ID_TYPE_UHS1000

                // Model: UHS1000-001
                if (command.Equals("*R12", StringComparison.OrdinalIgnoreCase)) return "P#12=0x31534855"; // "UHS1"
                if (command.Equals("*R13", StringComparison.OrdinalIgnoreCase)) return "P#13=0x2D303030"; // "-000"
                if (command.Equals("*R14", StringComparison.OrdinalIgnoreCase)) return "P#14=0x313030"; // "001"
                if (command.Equals("*R15", StringComparison.OrdinalIgnoreCase)) return "P#15=0x00000000"; // "empty"

                // *V: return HUMIDITY + TEMP
                if (command.Equals("*V", StringComparison.OrdinalIgnoreCase))
                {
                    if (dataProvider.HasData)
                    {
                        var d = dataProvider.Next();
                        try
                        {
                            return ResponseBuilder.BuildH(d.VEL, d.TEMP); // VEL column is used for humidity
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("BuildH failed: " + ex.Message);
                            return "*V<SD>\r\n<HUM Units=\"%RH\">60.00</HUM>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                        }
                    }
                    return "*V<SD>\r\n<HUM Units=\"%RH\">60.00</HUM>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                }
            }

            // ======================================
            // ✅ UAS DEFAULT HANDLING
            // ======================================
            if (command.StartsWith("*R", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(command.Substring(2), out int reg))
                {
                    string key = $"P#{reg}";
                    if (regMap.TryGetValue(key, out var val))
                    {
                        if (reg >= 4 && reg <= 11) Thread.Sleep(30);
                        return $"{key}={val}";
                    }
                    return $"{key}=0x00000000";
                }
            }

            switch (command)
            {
                case "*RS":
                case "*RD":
                case "*RE":
                case "*RT":
                    return "OK";

                case "*V":
                    if (dataProvider.HasData)
                    {
                        var d = dataProvider.Next();
                        try
                        {
                            return ResponseBuilder.BuildV(d.VEL, d.TEMP);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("BuildV failed: " + ex.Message);
                            return "*V<SD>\r\n<VEL Units=\"m/s\">0.500</VEL>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                        }
                    }
                    return "*V<SD>\r\n<VEL Units=\"m/s\">0.500</VEL>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
            }

            return null;
        }
    }
}
