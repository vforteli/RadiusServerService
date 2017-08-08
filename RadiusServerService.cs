using log4net;
using System;
using System.IO;
using System.Net;
using System.ServiceProcess;
using Flexinets.Radius.Core;

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
                _log.Info("Reading configuration");
                var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "\\dictionary";
                var dictionary = new RadiusDictionary(path);
                _log.Info("Configuration read");

                _authenticationServer = new RadiusServer(new IPEndPoint(IPAddress.Any, 1812), dictionary, RadiusServerType.Authentication);
                _accountingServer = new RadiusServer(new IPEndPoint(IPAddress.Any, 1813), dictionary, RadiusServerType.Accounting);

                var packetHandler = new TestPacketHandler();
                _authenticationServer.AddPacketHandler(IPAddress.Parse("127.0.0.1"), "secret", packetHandler);
                _accountingServer.AddPacketHandler(IPAddress.Parse("127.0.0.1"), "secret", packetHandler);

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
