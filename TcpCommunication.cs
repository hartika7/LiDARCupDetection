using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiDARCupDetection
{

    class TcpCommunication
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ObjectDetector _objectDetector;
        private int _tcpPort;

        public TcpCommunication(ObjectDetector objectDetector)
        {
            _objectDetector = objectDetector;

            Configure();
            Task.Run(() => Run());
        }

        private void Configure()
        {
            Logger.Debug($"Configuring");

            try
            {
                var settings = (NameValueCollection)ConfigurationManager.GetSection("TcpCommunicationSettings").NotNull();

                _tcpPort = int.Parse(settings["TcpPort"].NotNull());
                Logger.Debug($"Using TcpPort: {_tcpPort}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Configuration failed");
                throw;
            }
        }

        public int GetPort()
        {
            return _tcpPort;
        }

        private void Run()
        {
            TcpListener tcpServer;

            try
            {
                tcpServer = new TcpListener(IPAddress.Any, _tcpPort);
                tcpServer.Start();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error starting TCP server");
                throw;
            }

            while (true)
            {
                try
                {
                    using (var tcpClient = tcpServer.AcceptTcpClient())
                    using (var tcpStream = tcpClient.GetStream())
                    {
                        Logger.Info($"New connection {tcpClient.Client.RemoteEndPoint}");

                        while (tcpClient.Connected)
                        {
                            var commandBytes = new byte[1024];
                            var commandLength = tcpStream.Read(commandBytes, 0, commandBytes.Length);
                            var commandString = Encoding.UTF8.GetString(commandBytes).Trim().Substring(0, commandLength);
                            Logger.Info($"Received from {tcpClient.Client.RemoteEndPoint}: '{commandString}'");

                            string responseString = GetResponse(commandString);
                            Logger.Info($"Response: '{responseString}'");

                            var responseBytes = Encoding.UTF8.GetBytes(responseString);
                            tcpStream.Write(responseBytes, 0, responseBytes.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error in TCP connection");
                }
            }
        }

        private string GetResponse(string command)
        {
            switch (command)
            {
                case "ActiveObjects()":
                    return _objectDetector.GetActiveObjects().ToJSON(false);
                default:
                    return "Unknown command";
            }
        }
    }
}
