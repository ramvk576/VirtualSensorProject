using SensorEmulator.Core;

namespace SensorEmulator.Core
{
    public static class ResponseBuilder
    {
        public static string BuildV(string vel, string temp)
        {
            string payload =
                "<SD>\r\n" +
                $"<VEL Units=\"m/s\">{vel}</VEL>\r\n" +
                $"<TEMP Units=\"C\">{temp}</TEMP>\r\n" +
                "</SD>";

            ushort crc = CRCUtility.Compute(payload);

            return "*V" + payload + $"CRC=0x{crc:X4}\r\n";
        }

        public static string BuildT(string temp)
        {
            string payload =
                "<SD>\r\n" +                
                $"<TEMP1 Units=\"C\">{temp}</TEMP>\r\n" +
                "</SD>";

            ushort crc = CRCUtility.Compute(payload);

            return "*V" + payload + $"CRC=0x{crc:X4}\r\n";
            
        }

        public static string BuildH(string hum, string temp)
        {
            string payload =
                "<SD>\r\n" +
                $"<HUM Units=\"%RH\">{hum}</HUM>\r\n" +
                $"<TEMP Units=\"C\">{temp}</TEMP>\r\n" +
                "</SD>";

            ushort crc = CRCUtility.Compute(payload);
            return "*V" + payload + $"CRC=0x{crc:X4}\r\n";
        }

    }
}
