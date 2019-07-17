using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiDARCupDetection
{
    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static ObjectDetector _objectDetector;
        private static TcpCommunication _tcpCommunication;

        static void Main(string[] args)
        {
            Logger.Info("Application started");

            _objectDetector = new ObjectDetector();
            _tcpCommunication = new TcpCommunication(_objectDetector);

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
                       Console.WriteLine(_tcpCommunication.GetPort());
                        break;
                    case "objects":
                        Console.WriteLine(_objectDetector.GetObjects().ToJSON());
                        break;
                    case "active":
                        Console.WriteLine(_objectDetector.GetActiveObjects().ToJSON());
                        break;
                    case "auto":
                        Console.WriteLine(_objectDetector.GetAutodetected().ToJSON());
                        break;
                    case "autoconfig":
                        Console.WriteLine(_objectDetector.GetAutodetectConfiguration().ToJSON());
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
            Application.Run(new MainWindow(_objectDetector));
        }

        private static void PrintHelp()
        {
            var helpText = "Commands:\n" +
                "help, gui, port, objects, active, auto, autoconfig";
            Console.WriteLine(helpText);
        }
    }
}
