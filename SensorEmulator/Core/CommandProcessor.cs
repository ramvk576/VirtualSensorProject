using System;
using System.Collections.Generic;
using System.Threading;

namespace SensorEmulator.Core
{
    public class CommandProcessor
    {
        private readonly LiveDataProvider dataProvider;
        private readonly Dictionary<string, string> regMap;

        public CommandProcessor(LiveDataProvider provider, Dictionary<string, string> registers)
        {
            dataProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            regMap = registers ?? BuildDefaultRegisters();
        }

        private static Dictionary<string, string> BuildDefaultRegisters()
        {
            // Fallback to your original single-sensor constants
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
                ["P#16"] = "0x00000000",
                ["P#17"] = "0x00000000",
                ["P#18"] = "0x00000000",
                ["P#19"] = "0x00000000",

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

            // Handle register reads centrally via regMap, preserving your *R timing
            if (command.StartsWith("*R", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(command.Substring(2), out int reg))
                {
                    string key = $"P#{reg}";
                    if (regMap.TryGetValue(key, out var val))
                    {
                        // preserve the little sleeps you had for R4..R11
                        if (reg >= 4 && reg <= 11) Thread.Sleep(30);
                        return $"{key}={val}";
                    }

                    // Unknown register: safe zero
                    return $"{key}=0x00000000";
                }
            }

            switch (command)
            {
                // Keep your ancillary commands unchanged
                case "*RS":
                case "*RD":
                case "*RE":
                case "*RT":
                    return "OK";

                case "*V":
                    if (dataProvider.HasData)
                    {
                        var data = dataProvider.Next();
                        // Your existing builder + firmware CRC logic stays untouched
                        return ResponseBuilder.BuildV(data.VEL, data.TEMP);
                    }
                    // Safe placeholder exactly as your current code
                    return "*V<SD>\r\n<VEL Units=\"m/s\">0.500</VEL>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
            }

            return null;
        }
    }
}
