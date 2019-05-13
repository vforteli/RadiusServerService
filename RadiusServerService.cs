using Flexinets.Radius.Core;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceProcess;

namespace Flexinets.Radius
{
    public partial class RadiusServerService : ServiceBase
    {
        private RadiusServer _authenticationServer;
        private NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public RadiusServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _log.Info($"Starting RadiusServerService build version {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}");
                _log.Info("Reading configuration");

                var loggerFactory = new LoggerFactory();
                loggerFactory.AddNLog();
                var dictionary = new RadiusDictionary(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "\\content\\radius.dictionary", loggerFactory.CreateLogger<RadiusDictionary>());


                var radiusPacketParser = new RadiusPacketParser(loggerFactory.CreateLogger<RadiusPacketParser>(), dictionary);
                var packetHandler = new TestPacketHandler();
                var repository = new PacketHandlerRepository();

                repository.AddPacketHandler(IPAddress.Parse("127.0.0.1"), packetHandler, "secret");

                _authenticationServer = new RadiusServer(
                    new Net.UdpClientFactory(),
                    new IPEndPoint(IPAddress.Any, 1812),
                    radiusPacketParser,
                    RadiusServerType.Authentication,
                    repository,
                    loggerFactory.CreateLogger<RadiusServer>());


                _log.Info("Configuration read");

                _authenticationServer.Start();
            }
            catch (Exception ex)
            {
                _log.Fatal(ex, "Failed to start service");
                throw;
            }
        }

        protected override void OnStop()
        {
            _authenticationServer?.Stop();
            _authenticationServer?.Dispose();
        }
    }
}
