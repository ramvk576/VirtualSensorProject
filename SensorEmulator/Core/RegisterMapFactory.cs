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

        private static List<string> Split4(string s)
        {
            var list = new List<string>();
            int i = 0;

            while (i < s.Length)
            {
                int take = Math.Min(4, s.Length - i);
                list.Add(s.Substring(i, take));
                i += take;
            }

            while (list.Count < 4)
                list.Add("");

            return list;
        }

        public static Dictionary<string, string> BuildUas(SensorProfile p)
        {
            string sn = p.SerialNumber ?? "";
            var chunks = Split4(sn);

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["P#1"] = "0x00000007", // UAS type

                ["P#4"] = Ascii4ToHexLE(chunks[0]),
                ["P#5"] = Ascii4ToHexLE(chunks[1]),
                ["P#6"] = Ascii4ToHexLE(chunks[2]),
                ["P#7"] = Ascii4ToHexLE(chunks[3]),

                ["P#8"] = "0x00000000",
                ["P#9"] = "0x00000000",
                ["P#10"] = "0x00000000",
                ["P#11"] = "0x00000000",

                ["P#12"] = "0x31534155", // "UAS1"
                ["P#13"] = "0x2D303031", // "-001"
                ["P#14"] = "0x54676E45", // "EngT"
                ["P#15"] = "0x00747365", // "est\0"

                ["P#30"] = "0x07E40515",
                ["P#31"] = "0x000001F4",
                ["P#32"] = "0x000001F4",
                ["P#33"] = "0x00000000",
                ["P#34"] = "0x00000000",

                ["P#145"] = "0x00030040"
            };
        }

        public static Dictionary<string, string> BuildUts(SensorProfile p)
        {
            string sn = p.SerialNumber ?? "";
            var chunks = Split4(sn);

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["P#1"] = "0x00000003", // UTS type id

                ["P#4"] = Ascii4ToHexLE(chunks[0]),
                ["P#5"] = Ascii4ToHexLE(chunks[1]),
                ["P#6"] = Ascii4ToHexLE(chunks[2]),
                ["P#7"] = Ascii4ToHexLE(chunks[3]),

                // Model "UTS1000T-001"
                ["P#12"] = "0x31535455", // "UTS1"
                ["P#13"] = "0x54303030", // "T000"
                ["P#14"] = "0x3130302D", // "-001"
                ["P#15"] = "0x00000000",

                ["P#30"] = "0x07EA0515",
                ["P#31"] = "0x000001F4",
                ["P#32"] = "0x000001F4",
                ["P#33"] = "0x00000000",
                ["P#34"] = "0x00000000",

                ["P#145"] = "0x00030040"
            };
        }

        public static Dictionary<string, string> BuildUhs(SensorProfile p)
        {
            string sn = p.SerialNumber ?? "";
            var chunks = Split4(sn);

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["P#1"] = "0x00000004", // UHS type id

                ["P#4"] = Ascii4ToHexLE(chunks[0]),
                ["P#5"] = Ascii4ToHexLE(chunks[1]),
                ["P#6"] = Ascii4ToHexLE(chunks[2]),
                ["P#7"] = Ascii4ToHexLE(chunks[3]),

                // Model "UHS1000-001"
                ["P#12"] = Ascii4ToHexLE("UHS1"),
                ["P#13"] = Ascii4ToHexLE("0000"),
                ["P#14"] = Ascii4ToHexLE("-001"),
                ["P#15"] = "0x00000000",

                ["P#30"] = "0x07EA0515",
                ["P#31"] = "0x000001F4",
                ["P#32"] = "0x000001F4",
                ["P#33"] = "0x00000000",
                ["P#34"] = "0x00000000",

                ["P#145"] = "0x00030040"
            };
        }
    }
}
