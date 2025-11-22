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

            // Choose register map based on serial prefix
            var serial = profile.SerialNumber ?? "";
            System.Collections.Generic.Dictionary<string, string> regMap;

            if (serial.StartsWith("1716") || serial.StartsWith("UTS"))
                regMap = RegisterMapFactory.BuildUts(profile);
            else if (serial.StartsWith("1508") || serial.StartsWith("UHS"))
                regMap = RegisterMapFactory.BuildUhs(profile);
            else
                regMap = RegisterMapFactory.BuildUas(profile);

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

            var processor = new CommandProcessor(provider, regMap, profile.SerialNumber);
            var handler = new SerialHandler(port, processor);
            handler.Start();
        }
    }
}
