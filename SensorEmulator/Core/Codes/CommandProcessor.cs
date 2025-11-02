using SensorEmulator.Core;
using System;
using System.Threading;

namespace SensorEmulator.Core
{
    public class CommandProcessor
    {
        private readonly LiveDataProvider dataProvider;

        public CommandProcessor(LiveDataProvider provider)
        {
            dataProvider = provider;
        }

        public string Process(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return null;

            command = command.Trim();

            // Handle *RPage->: remap to *R
            if (command.StartsWith("*RPage->:", StringComparison.Ordinal))
                command = "*R" + command.Substring(10);

            switch (command)
            {
                case "*R1": return "P#1=0x00000007";

                case "*R4": Thread.Sleep(30); return "P#4=0x35333231";
                case "*R5": Thread.Sleep(30); return "P#5=0x3530312D";
                case "*R6": Thread.Sleep(30); return "P#6=0x36323539";
                case "*R7": Thread.Sleep(30); return "P#7=0x3730302D";
                case "*R8": Thread.Sleep(30); return "P#8=0x00000000";
                case "*R9": Thread.Sleep(30); return "P#9=0x00000000";
                case "*R10": Thread.Sleep(30); return "P#10=0x00000000";
                case "*R11": Thread.Sleep(30); return "P#11=0x00000000";

                case "*R12": return "P#12=0x31534155";
                case "*R13": return "P#13=0x2D303031";
                case "*R14": return "P#14=0x54676E45";
                case "*R15": return "P#15=0x00747365";
                case "*R16": return "P#16=0x00000000";
                case "*R17": return "P#17=0x00000000";
                case "*R18": return "P#18=0x00000000";
                case "*R19": return "P#19=0x00000000";

                case "*R30": return "P#30=0x07E40515";
                case "*R31": return "P#31=0x000001F4";
                case "*R32": return "P#32=0x000001F4";
                case "*R33": return "P#33=0x00000000";
                case "*R34": return "P#34=0x00000000";

                case "*R145": return "P#145=0x00030040";

                case "*V":
                    if (dataProvider.HasData)
                    {
                        var data = dataProvider.Next();
                        return ResponseBuilder.BuildV(data.VEL, data.TEMP);
                    }
                    return "*V<SD>\r\n<VEL Units=\"m/s\">0.500</VEL>\r\n<TEMP Units=\"C\">28.50</TEMP>\r\n</SD>CRC=0xDB46\r\n";

                case "*RS":
                case "*RD":
                case "*RE":
                case "*RT":
                    return "OK";

                default:
                    return null;
            }
        }
    }
}
