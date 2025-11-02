using System;
using System.Collections.Generic;
using System.Text;

namespace SensorEmulator.Core
{
    public static class RegisterMapFactory
    {
        private static string Ascii4ToHexLE(string four)
        {
            if (string.IsNullOrEmpty(four)) four = "";
            if (four.Length != 4) four = (four + "    ").Substring(0, 4);
            byte[] b = Encoding.ASCII.GetBytes(four);
            uint packed = (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
            return "0x" + packed.ToString("X8");
        }

        public static Dictionary<string, string> BuildUas(SensorProfile p)
        {
            // Base serial chunks: "1235-1059526-###"
            string ch4 = "1235";
            string ch5 = "-105";
            string ch6 = "9526";
            string last3 = p.KeepLegacyCom25 ? "007" : p.SerialSuffix.ToString("000");
            string ch7 = "-" + last3;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["P#1"] = "0x00000007",

                ["P#4"] = Ascii4ToHexLE(ch4),
                ["P#5"] = Ascii4ToHexLE(ch5),
                ["P#6"] = Ascii4ToHexLE(ch6),
                ["P#7"] = Ascii4ToHexLE(ch7),

                ["P#8"] = "0x00000000",
                ["P#9"] = "0x00000000",
                ["P#10"] = "0x00000000",
                ["P#11"] = "0x00000000",

                // Model split kept identical to your working values
                ["P#12"] = "0x31534155", // "UAS1"
                ["P#13"] = "0x2D303031", // "-001"
                ["P#14"] = "0x54676E45", // "-Eng"
                ["P#15"] = "0x00747365", // "Test"

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

            return map;
        }
    }
}
