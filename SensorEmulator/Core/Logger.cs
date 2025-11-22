using System;
using System.IO;

namespace SensorEmulator.Core
{
    public static class Logger
    {
        private static readonly string logFile = "SensorLog.txt";
        private static readonly object lockObj = new object();

        private const long MaxLogSizeBytes = 2_000_000; // 2MB limit

        public static void Log(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(entry);

            lock (lockObj)
            {
                try
                {
                    // If file exceeds max limit → truncate
                    if (File.Exists(logFile))
                    {
                        var info = new FileInfo(logFile);
                        if (info.Length > MaxLogSizeBytes)
                        {
                            File.WriteAllText(logFile, string.Empty);
                        }
                    }

                    File.AppendAllText(logFile, entry + Environment.NewLine);
                }
                catch
                {
                    // Silent catch: logging should NEVER break emulator
                }
            }
        }
    }
}













//using System;
//using System.IO;

//namespace SensorEmulator.Core
//{
//    public static class Logger
//    {
//        private static readonly string logFile = "SensorLog.txt";
//        private static readonly object lockObj = new object();


//        public static void Log(string message)
//        {
//            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
//            Console.WriteLine(entry);

//            lock (lockObj)
//            {
//                File.AppendAllText(logFile, entry + Environment.NewLine);
//            }
//        }
//    }
//}
