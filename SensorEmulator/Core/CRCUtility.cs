using System.Text;

namespace SensorEmulator.Core
{
    public static class CRCUtility
    {
        public static ushort Compute(string input)
        {
            ushort crc = 0xFFFF;
            byte[] data = Encoding.ASCII.GetBytes(input);

            foreach (byte tx in data)
            {
                int temp = (crc >> 8) ^ tx;
                crc = (ushort)((crc << 8) & 0xFFFF);
                int quick = temp ^ (temp >> 4);
                crc ^= (ushort)(quick & 0xFFFF);
                quick <<= 5;
                crc ^= (ushort)(quick & 0xFFFF);
                quick <<= 7;
                crc ^= (ushort)(quick & 0xFFFF);
            }

            return crc;
        }
    }
}
