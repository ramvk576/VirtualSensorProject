using System.IO.Ports;

namespace SensorEmulator.Core
{
    public sealed class SensorSession
    {
        private readonly SensorProfile profile;

        public SensorSession(SensorProfile p)
        {
            profile = p;
        }

        public void Start()
        {
            var provider = new LiveDataProvider();
            provider.Load(profile.CsvPath);

            // Build register map for this sensor
            var regMap = RegisterMapFactory.BuildUas(profile);

            // Open serial on listener port
            var port = new SerialPort(profile.ListenPort, 19200, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                NewLine = "\r\n",
                Handshake = Handshake.None,
                DtrEnable = true,
                RtsEnable = true
            };
            port.Open();

            var processor = new CommandProcessor(provider, regMap);
            var handler = new SerialHandler(port, processor);
            handler.Start();
        }
    }
}
