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

        // UTS – TEMP1 only
        public static string BuildTemp1(string temp)
        {
            string payload =
                "<SD>\r\n" +
                $"<TEMP1 Units=\"C\">{temp}</TEMP1>\r\n" +
                "</SD>";

            ushort crc = CRCUtility.Compute(payload);
            return "*V" + payload + $"CRC=0x{crc:X4}\r\n";
        }

        // UHS – HUM + TEMP
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
