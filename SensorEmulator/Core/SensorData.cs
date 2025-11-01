using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorEmulator.Core
{
    public class SensorData
    {
        public string VEL { get; set; }
        public string TEMP { get; set; }

        public SensorData(string vel, string temp)
        {
            VEL = vel;
            TEMP = temp;
        }
    }
}

