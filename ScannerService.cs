using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace LiDARCupDetection
{
    public class ScannerService
    {
        private static int BUFFER_SIZE = 4096;
        private static int SKIP_BYTES_START = 1;
        private static int SKIP_BYTES_END = 2;
        private static int COUNT_POSITION = 25;
        private static int MEASUREMENTS_START_POSITION = COUNT_POSITION + 1; // 26

        public class Point {
            public double Th { get; set; }
            public double R { get; set; }
            public double X => R * Math.Sin(Th * Math.PI / 180);
            public double Y => R * Math.Cos(Th * Math.PI / 180);

            public Point(double th, double r)
            {
                Th = th;
                R = r;
            }
        }

        public class ScanResult
        {
            public bool Success { get; set; }
            public List<Point> Points { get; set; }

            public ScanResult()
            {
                Points = new List<Point>();
            }
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _scannerClient;
        private NetworkStream _scannerStream;
        private StreamWriter _scannerWriter;
        private bool _connecting = false;

        private string _scannerCommand { get; set; }
        private string _scannerIP { get; set; }
        private int _scannerPort { get; set; }
        private int _startAngle { get; set; }
        private int _stopAngle { get; set; }

        public ScannerService(string settingsPostfix)
        {
            Logger.Debug($"Started");

            Configure(settingsPostfix);
        }

        private void Configure(string settingsPostfix)
        {
            Logger.Debug($"Configuring ({settingsPostfix})");

            try
            {
                var settings = (NameValueCollection)ConfigurationManager.GetSection($"ScannerServiceSettings_{settingsPostfix}").NotNull();

                _scannerCommand = settings["ScannerCommand"].NotNull();
                Logger.Debug($"Using ScannerCommand: {_scannerCommand}");

                _scannerIP = settings["ScannerIP"].NotNull();
                Logger.Debug($"Using ScannerIP: {_scannerIP}");

                _scannerPort = int.Parse(settings["ScannerPort"].NotNull());
                Logger.Debug($"Using ScannerPort: {_scannerPort}");

                _startAngle = int.Parse(settings["StartAngle"].NotNull());
                Logger.Debug($"Using StartAngle: {_startAngle}");

                _stopAngle = int.Parse(settings["StopAngle"].NotNull());
                Logger.Debug($"Using StopAngle: {_stopAngle}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Configuration failed");
                throw;
            }
        }

        public async Task<bool> Connect()
        {
            if (_connecting)
            {
                return false;
            }

            Logger.Info($"Connecting to scanner at {_scannerIP}:{_scannerPort}");
            _connecting = true;

            try
            {
                Disconnect();

                _scannerClient = new TcpClient();
                await _scannerClient.ConnectAsync(_scannerIP, _scannerPort).ConfigureAwait(false);
                _scannerStream = _scannerClient.GetStream();
                _scannerWriter = new StreamWriter(_scannerStream, Encoding.ASCII);

                Logger.Info("Connected");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Connection failed");

            }

            _connecting = false;
            return IsConnected();
        }

        private void Disconnect()
        {
            if (_scannerClient != null)
            {
                _scannerClient.Close();
            }

            if (_scannerStream != null)
            {
                _scannerStream.Close();
            }

            if (_scannerWriter != null)
            {
                _scannerWriter.Close();
            }
        }

        public bool IsConnected()
        {
            return _scannerClient != null && _scannerClient.Connected;
        }

        public async Task<ScanResult> Poll()
        {
            try
            {
                if (!IsConnected() && !await Connect().ConfigureAwait(false))
                {
                    throw new Exception("Not connected");
                }

                _scannerWriter.Write($"\x02{_scannerCommand}\x03");
                _scannerWriter.Flush();

                byte[] receivedBytes = new byte[BUFFER_SIZE];
                int bytesCount = await _scannerStream.ReadAsync(receivedBytes, 0, receivedBytes.Length).ConfigureAwait(false);

                string[] scanData = Encoding.ASCII.GetString(receivedBytes, SKIP_BYTES_START, bytesCount - SKIP_BYTES_END).Split(' ');
                int pointCount = int.Parse(scanData[COUNT_POSITION], NumberStyles.HexNumber);

                double[] pointRadii = new double[pointCount];
                double[] pointAngles = Generate.LinearSpaced(pointCount, _startAngle, _stopAngle);
                
                for (int i = MEASUREMENTS_START_POSITION; i < MEASUREMENTS_START_POSITION + pointCount; i++)
                {
                    pointRadii[pointCount - 1 - (i - MEASUREMENTS_START_POSITION)] 
                        = int.Parse(scanData[i], NumberStyles.HexNumber);
                }

                List<Point> points = pointAngles
                    .Zip(pointRadii, (u, v) => new Point(u, v))
                    .ToList();

                return new ScanResult()
                {
                    Success = true,
                    Points = points
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Polling failed");
                return new ScanResult()
                {
                    Success = false
                };
            }
        }
    }
}
