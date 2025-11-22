using System;

namespace SensorEmulator.Core
{
    public class RandomLiveDataProvider
    {
        private readonly Random rnd;
        private readonly string serial;

        public RandomLiveDataProvider(string serialNumber)
        {
            serial = serialNumber ?? "";
            int seed = serial.GetHashCode() ^ Environment.TickCount;
            rnd = new Random(seed);
        }

        public bool HasData => true;

        public SensorData Next()
        {
            double vel = Math.Round(rnd.NextDouble() * 1.0, 2);
            double temp = Math.Round(20.0 + rnd.NextDouble() * 20.0, 2);

            return new SensorData(
                vel.ToString("F2"),
                temp.ToString("F2")
            );
        }
    }
}
