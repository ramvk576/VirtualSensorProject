using System;
using System.IO;

namespace SensorEmulator.Core
{
    public static class Logger
    {
        private static readonly string logFile = "SensorLog.txt";
        private static readonly object lockObj = new object();


        public static void Log(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(entry);

            lock (lockObj)
            {
                File.AppendAllText(logFile, entry + Environment.NewLine);
            }
        }
    }
}
