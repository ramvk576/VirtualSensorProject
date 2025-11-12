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

        public CommandProcessor(LiveDataProvider provider, Dictionary<string, string> registers, string serialNumber)
        {
            dataProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            regMap = registers ?? BuildDefaultRegisters();
            serial = serialNumber ?? string.Empty;

            // Detect UTS sensor either by “UTS...” prefix or by numeric ID (1716-...).
            isUts = serial.StartsWith("UTS", StringComparison.OrdinalIgnoreCase)
                    || serial.StartsWith("1716", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> BuildDefaultRegisters()
        {
            // UAS defaults (unchanged)
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

            // Normalize AccuTrac’s *RPage->:1 syntax
            if (command.StartsWith("*RPage->:", StringComparison.Ordinal))
                command = "*R" + command.Substring(10);

            //
            // ===============================
            // ✅ UTS SENSOR HANDLING
            // ===============================
            //
            if (isUts)
            {
                // Identification register (UTS1000T = 3)
                if (command.Equals("*R1", StringComparison.OrdinalIgnoreCase))
                    return "P#1=0x00000003";

                // Serial number mapping (from SensorProfile.SerialNumber)
                if (command.Equals("*R4", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R5", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R6", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R7", StringComparison.OrdinalIgnoreCase))
                {
                    var chunks = SplitToChunks(serial ?? string.Empty, 4);
                    while (chunks.Count < 4) chunks.Add(string.Empty);

                    int reg = int.Parse(command.Substring(2));
                    int idx = reg - 4; // *R4 -> index 0
                    string hex = Ascii4ToHexLE(chunks[idx]);
                    return $"P#{reg}={hex}";
                }

                // Model registers: consistent, fixed for all UTS units
                switch (command.ToUpperInvariant())
                {
                    case "*R12": return "P#12=0x31535455"; // "UTS1"
                    case "*R13": return "P#13=0x54303030"; // "T000"
                    case "*R14": return "P#14=0x3130302D"; // "-001"
                    case "*R15": return "P#15=0x00000000"; // blank
                }

                // UTS *V = TEMP ONLY (TEMP1)
                if (command.Equals("*V", StringComparison.OrdinalIgnoreCase))
                {
                    if (dataProvider.HasData)
                    {
                        var data = dataProvider.Next();
                        try
                        {
                            return ResponseBuilder.BuildT(data.TEMP);
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

            //
            // ===============================
            // ✅ UAS & COMMON SENSOR HANDLING
            // ===============================
            //
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
                        var data = dataProvider.Next();
                        try
                        {
                            return ResponseBuilder.BuildV(data.VEL, data.TEMP);
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

        // ============================================
        // Helper: convert ASCII -> 32-bit little-endian hex
        // ============================================
        private static string Ascii4ToHexLE(string four)
        {
            if (four == null) four = string.Empty;
            if (four.Length != 4) four = (four + "    ").Substring(0, 4);
            byte[] b = Encoding.ASCII.GetBytes(four);
            uint packed = (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
            return "0x" + packed.ToString("X8");
        }

        // ============================================
        // Helper: split string into 4-character chunks
        // ============================================
        private static List<string> SplitToChunks(string s, int chunkSize)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s))
            {
                for (int j = 0; j < 4; j++) list.Add(string.Empty);
                return list;
            }

            int pos = 0;
            while (pos < s.Length)
            {
                int take = Math.Min(chunkSize, s.Length - pos);
                list.Add(s.Substring(pos, take));
                pos += take;
            }

            return list;
        }
    }
}










/*using System;
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

        public CommandProcessor(LiveDataProvider provider, Dictionary<string, string> registers, string serialNumber)
        {
            dataProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            regMap = registers ?? BuildDefaultRegisters();
            serial = serialNumber ?? string.Empty;
            // UTS detection: either serial starts with UTS... or you pass the numeric UTS serial (e.g. 1716-...)
            isUts = serial.StartsWith("UTS", StringComparison.OrdinalIgnoreCase)
                    || serial.StartsWith("1716", StringComparison.OrdinalIgnoreCase); // keep flexible for your test serial
        }

        private static Dictionary<string, string> BuildDefaultRegisters()
        {
            // UAS defaults (leave unchanged)
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

            // Normalise AccuTrac page-style queries into simple *Rn
            if (command.StartsWith("*RPage->:", StringComparison.Ordinal))
                command = "*R" + command.Substring(10);

            //
            // UTS specific handling (non-invasive: returns early only for UTS-specific responses)
            //
            if (isUts)
            {
                // UTS: Identification register -> small integer id (use the same hex style)
                if (command.Equals("*R1", StringComparison.OrdinalIgnoreCase))
                {
                    // Many of your earlier logs showed a value '3' for UTS type.
                    // Present as 32-bit hex to match the format used elsewhere.
                    return "P#1=0x00000003";
                }

                // If the AccuTrac asks for serial/model registers, produce them from the provided serial / model.
                // Serial chunk registers P#4..P#7 are the ASCII 4-char chunks of the serial encoded little-endian as hex.
                if (command.Equals("*R4", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R5", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R6", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R7", StringComparison.OrdinalIgnoreCase))
                {
                    var chunks = SplitToChunks(serial ?? string.Empty, 4);
                    // Ensure we have 4 chunks
                    while (chunks.Count < 4) chunks.Add(string.Empty);
                    int idx = int.Parse(command.Substring(2)) - 4; // *R4 -> index 0
                    string hex = Ascii4ToHexLE(chunks[idx]);
                    return $"P#{"4" + idx}={hex}".Replace("4" + idx, (4 + idx).ToString()); // produce P#4..P#7
                }

                // Model registers: produce UTS model in the same 4-chunk ASCII->hexLE format so AccuTrac renders correctly
                // Example desired model: "UTS1000T-001"
                if (command.Equals("*R12", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R13", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R14", StringComparison.OrdinalIgnoreCase) ||
                    command.Equals("*R15", StringComparison.OrdinalIgnoreCase))
                {
                    // Hardcode the UTS model mapping (safe and matches your test)
                    // P#12 = "UTS1"  -> 0x55545331
                    // P#13 = "000T"  -> 0x30303054
                    // P#14 = "-001"  -> 0x2D303031
                    // P#15 = 0x00000000
                    switch (command.ToUpperInvariant())
                    {
                        case "*R12": return "P#12=0x31535455";
                        case "*R13": return "P#13=0x54303030";
                        case "*R14": return "P#14=0x3130302D";
                        case "*R15": return "P#15=0x00000000";
                    }
                }

                // UTS: *V must return TEMP only (TEMP1)
                if (command.Equals("*V", StringComparison.OrdinalIgnoreCase))
                {
                    if (dataProvider.HasData)
                    {
                        var data = dataProvider.Next();
                        try
                        {
                            return ResponseBuilder.BuildT(data.TEMP);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("BuildT failed: " + ex.Message);
                            // fallback: safe placeholder
                            return "*V<SD>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                        }
                    }
                    return "*V<SD>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                }

                }
            
            //
            // Non-UTS: existing UAS register handling and *V behaviour
            //
            if (command.StartsWith("*R", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(command.Substring(2), out int reg))
                {
                    string key = $"P#{reg}";
                    if (regMap.TryGetValue(key, out var val))
                    {
                        // preserve original timings used for some registers
                        if (reg >= 4 && reg <= 11) Thread.Sleep(30);
                        return $"{key}={val}";
                    }

                    // Unknown register: return zeros (safe)
                    return $"{key}=0x00000000";
                }
            }

            // UAS other commands and *V (unchanged)
            switch (command)
            {
                case "*RS":
                case "*RD":
                case "*RE":
                case "*RT":
                    return "OK";

                case "*V": // UAS path: full VEL+TEMP using ResponseBuilder
                    if (dataProvider.HasData)
                    {
                        var data = dataProvider.Next();
                        try
                        {
                            return ResponseBuilder.BuildV(data.VEL, data.TEMP);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("BuildV failed: " + ex.Message);
                            // fallback: safe placeholder
                            return "*V<SD>\r\n<VEL Units=\"m/s\">0.500</VEL>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
                        }
                    }
                    return "*V<SD>\r\n<VEL Units=\"m/s\">0.500</VEL>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";
            }

            return null;
        }

        // Helpers

        private static string Ascii4ToHexLE(string four)
        {
            if (four == null) four = string.Empty;
            if (four.Length != 4) four = (four + "    ").Substring(0, 4);
            byte[] b = Encoding.ASCII.GetBytes(four);
            uint packed = (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
            return "0x" + packed.ToString("X8");
        }

        private static List<string> SplitToChunks(string s, int chunkSize)
        {
            var outList = new List<string>();
            if (string.IsNullOrEmpty(s))
            {
                for (int j = 0; j < 4; j++) outList.Add(string.Empty);
                return outList;
            }

            int pos = 0;
            while (pos < s.Length)
            {
                int take = Math.Min(chunkSize, s.Length - pos);
                outList.Add(s.Substring(pos, take));
                pos += take;
            }

            return outList;
        }

    }
}
*/
