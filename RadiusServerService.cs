using FlexinetsDBEF;
using log4net;
using Microsoft.Azure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Flexinets.Radius
{
    public partial class RadiusServerService : ServiceBase
    {
        private FlexinetsEntitiesFactory _contextFactory;
        private RadiusServer _rsauth;
        private RadiusServer _rsacct;
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
                _contextFactory = new FlexinetsEntitiesFactory(CloudConfigurationManager.GetSetting("SQLConnectionString"));
                var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\dictionary";  // todo hurgh
                var dictionary = new RadiusDictionary(path);
                var port = Convert.ToInt32(CloudConfigurationManager.GetSetting("Port"));
                var ipassSecret = CloudConfigurationManager.GetSetting("ipasssecret");
                var mbbSecret = CloudConfigurationManager.GetSetting("mbbsecret");
                var disconnectCheckerPath = CloudConfigurationManager.GetSetting("DisconnectCheckerPath");
                var welcomeSenderPath = CloudConfigurationManager.GetSetting("WelcomeSenderPath");
                var apiUrl = CloudConfigurationManager.GetSetting("ApiUrl");
                _log.Info("Configuration read");


                _rsauth = new RadiusServer(new IPEndPoint(IPAddress.Any, port), dictionary);
                _rsacct = new RadiusServer(new IPEndPoint(IPAddress.Any, port + 1), dictionary);    // todo, good grief...

                var ipassPacketHandler = new iPassPacketHandler(_contextFactory);
                _rsauth.AddPacketHandler(IPAddress.Parse("127.0.0.1"), ipassSecret, ipassPacketHandler);
                _rsacct.AddPacketHandler(IPAddress.Parse("127.0.0.1"), ipassSecret, ipassPacketHandler);


                var networkIdProvider = new NetworkIdProvider(_contextFactory, apiUrl);
                var mbbPacketHandler = new MobileDataPacketHandler(_contextFactory, networkIdProvider, disconnectCheckerPath, welcomeSenderPath);

                _rsauth.AddPacketHandler(IPAddress.Parse("10.50.0.253"), mbbSecret, mbbPacketHandler);
                _rsauth.AddPacketHandler(IPAddress.Parse("10.50.0.254"), mbbSecret, mbbPacketHandler);

                _rsacct.AddPacketHandler(IPAddress.Parse("10.50.0.253"), mbbSecret, mbbPacketHandler);
                _rsacct.AddPacketHandler(IPAddress.Parse("10.50.0.254"), mbbSecret, mbbPacketHandler);

                _rsauth.Start();
                _rsacct.Start();
            }
            catch (Exception ex)
            {
                _log.Fatal("Failed to start service", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            _rsauth.Stop();
            _rsauth.Dispose();
            _rsacct.Stop();
            _rsacct.Dispose();
        }
    }
}
