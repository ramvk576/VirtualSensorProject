namespace SensorEmulator.Core
{
    public class SensorData
    {
        public string VEL { get; }
        public string TEMP { get; }

        public SensorData(string vel, string temp)
        {
            VEL = vel;
            TEMP = temp;
        }
    }
}
