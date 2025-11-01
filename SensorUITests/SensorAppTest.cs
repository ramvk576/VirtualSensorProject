using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SensorUITests
{
    [TestClass]
    public class SensorAppTest
    {
        private WindowsDriver<WindowsElement> session;
        private Process emulatorProc;

        private readonly string settingID = "btnSettings";
        private readonly string cportAndSensorConnectID = "btnNetWorkSettings";
        private readonly string localUSBID = "btnLocalUSB";
        private readonly string playReading = "btnStart";
        private readonly string closeButtonID = "btnClose";

        [TestInitialize]
        public void Setup()
        {
            // Kill existing emulator & WinAppDriver
            foreach (var proc in Process.GetProcessesByName("SensorEmulator"))
            {
                try { proc.Kill(); } catch { }
            }
            foreach (var proc in Process.GetProcessesByName("WinAppDriver"))
            {
                try { proc.Kill(); } catch { }
            }

            // Paths
            string emulatorPath = @"C:\Users\ramesh.s\source\repos\SensorAutomation\SensorEmulator\bin\Debug\SensorEmulator.exe";
            string winAppDriverPath = @"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe";

            // Start Emulator
            var startInfo = new ProcessStartInfo
            {
                FileName = emulatorPath,
                Arguments = "COM26",
                WorkingDirectory = Path.GetDirectoryName(emulatorPath),
                UseShellExecute = false,
                CreateNoWindow = false
            };

            Console.WriteLine($"Starting emulator from: {emulatorPath}");
            emulatorProc = Process.Start(startInfo);
            Thread.Sleep(3000);

            if (emulatorProc == null || emulatorProc.HasExited)
                Assert.Fail("❌ Sensor Emulator failed to start or exited unexpectedly.");

            // Start WinAppDriver
            Process.Start(winAppDriverPath);
            Thread.Sleep(2000);

            // Launch AccuTrac
            AppiumOptions opts = new AppiumOptions();
            opts.AddAdditionalCapability("deviceName", "WindowsPC");
            opts.AddAdditionalCapability("app",
                @"C:\Users\ramesh.s\source\repos\AccuTrac Pro\source\AccuTrac Pro\bin\Debug\AccuTrac.exe");

            session = new WindowsDriver<WindowsElement>(
                new Uri("http://127.0.0.1:4723"), opts
            );
            Thread.Sleep(4000);

            // Handle license popup
            try
            {
                var okButton = session.FindElementByName("OK");
                if (okButton != null)
                {
                    okButton.Click();
                    Thread.Sleep(4000);

                    for (int i = 0; i < 10; i++)
                    {
                        var handles = session.WindowHandles;
                        if (handles.Count > 1)
                        {
                            session.SwitchTo().Window(handles.Last());
                            break;
                        }
                        Thread.Sleep(1000);
                    }

                    session.SwitchTo().Window(session.WindowHandles.Last());
                    Thread.Sleep(2000);
                }
            }
            catch { Console.WriteLine("No license popup found."); }
        }

        [TestMethod]
        public void Test_SensorDataFlow()
        {
            try
            {
                session.SwitchTo().Window(session.WindowHandles.Last());

                var SettingsButton = session.FindElementByAccessibilityId(settingID);
                SettingsButton.Click();
                Console.WriteLine("Clicked Settings Button successfully.");
                Thread.Sleep(1000);

                var USBSelectionButton = session.FindElementByAccessibilityId(cportAndSensorConnectID);
                USBSelectionButton.Click();
                Console.WriteLine("Clicked USB Selection Button successfully.");
                Thread.Sleep(1000);

                var LocalUSBButton = session.FindElementByAccessibilityId(localUSBID);
                LocalUSBButton.Click();
                Console.WriteLine("Clicked Local USB Selection Button successfully.");
                Thread.Sleep(8000); // wait for sensor detection

                // ✅ Step 3: Click "Next" to complete USB configuration
                try
                {
                    var NextButton = session.FindElementByAccessibilityId("btnApply");
                    NextButton.Click();
                    Console.WriteLine("Clicked 'Next' button successfully on USB screen.");
                    Thread.Sleep(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠️ Could not find or click 'Next' button: " + ex.Message);
                }

                // ✅ Step 4: Click "Main Screen" to return to home
                try
                {
                    var MainScreenButton = session.FindElementByAccessibilityId("btnApply");
                    MainScreenButton.Click();
                    Console.WriteLine("Clicked 'Main Screen' button successfully.");
                    Thread.Sleep(4000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠️ Could not find or click 'Main Screen' button: " + ex.Message);
                }

                // ✅ Step 5: Wait for sensor details to appear before clicking Play
                Console.WriteLine("Waiting for main screen to refresh sensor context...");

                bool sensorReady = false;
                for (int i = 0; i < 15; i++) // wait up to ~15 seconds
                {
                    try
                    {
                        // Look for the sensor Serial Number text element
                        var serialCell = session.FindElementByName("Serial Number");
                        if (serialCell != null)
                        {
                            Console.WriteLine("Sensor context detected on main screen.");
                            sensorReady = true;
                            break;
                        }
                    }
                    catch
                    {
                        Thread.Sleep(1000);
                    }
                }

                // Give a short buffer delay for COM stabilization
                Thread.Sleep(2000);

                try
                {
                    session.SwitchTo().Window(session.WindowHandles.Last());
                    Console.WriteLine("Focus switched to main AccuTrac window.");

                    var PlayButton = session.FindElementByAccessibilityId("btnStart");
                    PlayButton.Click();
                    Console.WriteLine("Clicked Play button successfully.");

                    Thread.Sleep(8000); // Allow sensor to poll for *V
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠️ Could not find or click 'Play' button: " + ex.Message);
                }




                // ✅ Step 5: Validate Emulator Log
                string logPath = @"C:\Users\ramesh.s\source\repos\SensorAutomation\SensorEmulator\bin\Debug\SensorLog.txt";

                if (File.Exists(logPath))
                {
                    string logContent = File.ReadAllText(logPath);
                    Console.WriteLine("===== Sensor Emulator Log =====");
                    Console.WriteLine(logContent);
                    Console.WriteLine("================================");

                    // ✅ Updated validation: accept either *RPage or *V command
                    if (!logContent.Contains("*R") && !logContent.Contains("*V"))
                        Assert.Fail("❌ No *R or *V commands received. Sensor not detected or Play failed.");

                }
                else
                {
                    Assert.Fail("Test failed: SensorLog.txt not found. Ensure the Sensor Emulator is running.");
                }

            }
            catch (Exception ex)
            {
                Assert.Fail("Test failed: " + ex.Message);
            }
        }

        //[TestCleanup]
        //public void Cleanup()
        //{
        //    try
        //    {
        //        session?.Quit();
        //    }
        //    catch { }

        //    try
        //    {
        //        if (emulatorProc != null && !emulatorProc.HasExited)
        //        {
        //            emulatorProc.Kill();
        //            Console.WriteLine("Emulator process terminated.");
        //        }
        //    }
        //    catch { }
        //}
    }
}
