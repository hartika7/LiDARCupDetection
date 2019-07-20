using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiDARCupDetection
{
    class Program
    {
        private static string TIM561_KEY = "TIM561";
        private static string TIM361_KEY = "TIM361";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static ObjectDetector _objectDetectorTIM561;
        private static ObjectDetector _objectDetectorTIM361;
        private static TcpCommunication _tcpCommunicationTIM561;
        private static TcpCommunication _tcpCommunicationTIM361;

        static void Main(string[] args)
        {
            Logger.Info("Application started");

            _objectDetectorTIM561 = new ObjectDetector(TIM561_KEY, new ScannerService(TIM561_KEY));
            _objectDetectorTIM361 = new ObjectDetector(TIM361_KEY, new ScannerService(TIM361_KEY));
            _tcpCommunicationTIM561 = new TcpCommunication(TIM561_KEY, _objectDetectorTIM561);
            _tcpCommunicationTIM361 = new TcpCommunication(TIM361_KEY, _objectDetectorTIM361);

            Task.Run(() => UpdateTitle());

            Console.WriteLine("LiDAR Cup Detection for Drinkkirobotti 5.0");
            Console.WriteLine($"Serving on port: {_tcpCommunicationTIM561.GetPort()} (TIM561)");
            Console.WriteLine($"Serving on port: {_tcpCommunicationTIM361.GetPort()} (TIM361)\n");
            PrintHelp();

            while (true)
            {
                Console.Write(">");
                var command = Console.ReadLine();

                switch (command.ToLower())
                {
                    case "help":
                        PrintHelp();
                        break;
                    case "gui":
                        ShowGUI();
                        break;
                    case "port":
                        Console.WriteLine($"Serving on port: {_tcpCommunicationTIM561.GetPort()} (TIM561)");
                        Console.WriteLine($"Serving on port: {_tcpCommunicationTIM361.GetPort()} (TIM361)");
                        break;
                    case "objects":
                        Console.WriteLine("TIM561:");
                        Console.WriteLine(_objectDetectorTIM561.GetObjects().ToJSON());
                        Console.WriteLine("TIM361:");
                        Console.WriteLine(_objectDetectorTIM361.GetObjects().ToJSON());
                        break;
                    case "active":
                        Console.WriteLine("TIM561:");
                        Console.WriteLine(_objectDetectorTIM561.GetActiveObjects().ToJSON());
                        Console.WriteLine("TIM361:");
                        Console.WriteLine(_objectDetectorTIM361.GetActiveObjects().ToJSON());
                        break;
                    case "auto":
                        Console.WriteLine("TIM561:");
                        Console.WriteLine(_objectDetectorTIM561.GetAutodetected().ToJSON());
                        Console.WriteLine("TIM361:");
                        Console.WriteLine(_objectDetectorTIM361.GetAutodetected().ToJSON());
                        break;
                    case "autoconfig":
                        Console.WriteLine("TIM561:");
                        Console.WriteLine(_objectDetectorTIM561.GetAutodetectConfiguration().ToJSON());
                        Console.WriteLine("TIM361:");
                        Console.WriteLine(_objectDetectorTIM361.GetAutodetectConfiguration().ToJSON());
                        break;
                    default:
                        break;
                }
            }
        }

        [STAThread]
        private static void ShowGUI()
        {
            Logger.Debug("Showing GUI");

            Application.EnableVisualStyles();
            if (_objectDetectorTIM561.IsEnabled())
            {
                Task.Run(() => Application.Run(new MainWindow("TIM561", _objectDetectorTIM561)));
            }
            if (_objectDetectorTIM361.IsEnabled())
            {
                Task.Run(() => Application.Run(new MainWindow("TIM361", _objectDetectorTIM361)));
            }
        }

        private static void PrintHelp()
        {
            var helpText = "Commands:\n" +
                "help, gui, port, objects, active, auto, autoconfig";
            Console.WriteLine(helpText);
        }

        private static void UpdateTitle()
        {
            while (true)
            {
                var appName = Assembly.GetExecutingAssembly().GetName().Name;
                Console.Title = $"{appName} (TIM561: {GetDetectorStatus(_objectDetectorTIM561)}, TIM361: {GetDetectorStatus(_objectDetectorTIM361)})";
                Thread.Sleep(1000);
            }
        }

        private static string GetDetectorStatus(ObjectDetector objectDetector)
        {
            if (!objectDetector.IsEnabled())
            {
                return "Disabled";
            }
            return objectDetector.IsOnline() ? "Online" : "Offline";
        }
    }
}
