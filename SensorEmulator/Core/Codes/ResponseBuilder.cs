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
    }
}
