using System.Collections.Generic;
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
            Dictionary<string, string> regMap;

            if (profile.SerialNumber.StartsWith("1716") || profile.SerialNumber.StartsWith("UTS"))
                regMap = RegisterMapFactory.BuildUts(profile);
            else if (profile.SerialNumber.StartsWith("1508") || profile.SerialNumber.StartsWith("UHS"))
                regMap = RegisterMapFactory.BuildUhs(profile);
            else
                regMap = RegisterMapFactory.BuildUas(profile);


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

            // ✅ Updated line – now passes serial number to CommandProcessor
            var processor = new CommandProcessor(provider, regMap, profile.SerialNumber);
            var handler = new SerialHandler(port, processor);
            handler.Start();
        }
    }
}
