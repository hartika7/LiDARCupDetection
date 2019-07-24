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

        private static Dictionary<string, ObjectDetector> _objectDetectors; 
        private static TcpCommunication _tcpCommunication;

        static void Main(string[] args)
        {
            Logger.Info("Application started");

            _objectDetectors = new Dictionary<string, ObjectDetector>();
            _objectDetectors.Add(TIM561_KEY, new ObjectDetector(TIM561_KEY, new ScannerService(TIM561_KEY)));
            _objectDetectors.Add(TIM361_KEY, new ObjectDetector(TIM361_KEY, new ScannerService(TIM361_KEY)));

            _tcpCommunication = new TcpCommunication(_objectDetectors);

            Task.Run(() => UpdateTitle());

            Console.WriteLine("LiDAR Cup Detection for Drinkkirobotti 5.0");
            Console.WriteLine($"Serving on port: {_tcpCommunication.GetPort()}\n");
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
                        Console.WriteLine($"Serving on port: {_tcpCommunication.GetPort()}");
                        break;
                    case "objects":
                        foreach (var objectDetector in _objectDetectors)
                        {
                            Console.WriteLine($"{objectDetector.Key} ->");
                            Console.WriteLine(objectDetector.Value.GetObjects().ToJSON());
                        }
                        break;
                    case "active":
                        foreach (var objectDetector in _objectDetectors)
                        {
                            Console.WriteLine($"{objectDetector.Key} ->");
                            Console.WriteLine(objectDetector.Value.GetActiveObjects().ToJSON());
                        }
                        break;
                    case "auto":
                        foreach (var objectDetector in _objectDetectors)
                        {
                            Console.WriteLine($"{objectDetector.Key} ->");
                            Console.WriteLine(objectDetector.Value.GetAutodetected().ToJSON());
                        }
                        break;
                    case "autoconfig":
                        foreach (var objectDetector in _objectDetectors)
                        {
                            Console.WriteLine($"{objectDetector.Key} ->");
                            Console.WriteLine(objectDetector.Value.GetAutodetectConfiguration().ToJSON());
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }

        [STAThread]
        private static void ShowGUI()
        {
            Logger.Debug("Showing GUI");

            Application.EnableVisualStyles();
            foreach (var objectDetector in _objectDetectors)
            {
                if (objectDetector.Value.IsEnabled())
                {
                    Task.Run(() => Application.Run(new MainWindow(objectDetector.Key, objectDetector.Value)));
                }
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
                Console.Title = $"{appName} (TIM561: {GetDetectorStatus(_objectDetectors[TIM561_KEY])}, TIM361: {GetDetectorStatus(_objectDetectors[TIM361_KEY])})";
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
