using log4net;
using System;
using System.IO;
using System.Net;
using System.ServiceProcess;
using Flexinets.Radius.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Flexinets.Net;

namespace Flexinets.Radius
{
    public partial class RadiusServerService : ServiceBase
    {
        private RadiusServer _authenticationServer;
        private RadiusServer _accountingServer;
        private readonly ILog _log = LogManager.GetLogger(typeof(RadiusServerService));


        public RadiusServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            try
            {
                var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "/Content/radius.dictionary";
                var dictionary = new RadiusDictionary(path, NullLogger<RadiusDictionary>.Instance);
                var radiusPacketParser = new RadiusPacketParser(NullLogger<RadiusPacketParser>.Instance, dictionary);
                var packetHandler = new TestPacketHandler();
                var repository = new PacketHandlerRepository();
                var udpClientFactory = new UdpClientFactory();
                repository.AddPacketHandler(IPAddress.Any, packetHandler, "secret");

                _authenticationServer = new RadiusServer(
                    udpClientFactory,
                    new IPEndPoint(IPAddress.Any, 1812),
                    radiusPacketParser,
                    RadiusServerType.Authentication,
                    repository,
                    NullLogger<RadiusServer>.Instance);

                _accountingServer = new RadiusServer(
                   udpClientFactory,
                   new IPEndPoint(IPAddress.Any, 1813),
                   radiusPacketParser,
                   RadiusServerType.Accounting,
                   repository,
                   NullLogger<RadiusServer>.Instance);

                _authenticationServer.Start();
                _accountingServer.Start();
            }
            catch (Exception ex)
            {
                _log.Fatal("Failed to start service", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            _authenticationServer?.Stop();
            _authenticationServer?.Dispose();
            _accountingServer?.Stop();
            _accountingServer?.Dispose();
        }
    }
}
