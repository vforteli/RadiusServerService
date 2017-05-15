using Flexinets.MobileData.SMS;
using Flexinets.Radius.PacketHandlers;
using FlexinetsDBEF;
using log4net;
using Microsoft.Azure;
using System;
using System.IO;
using System.Net;
using System.ServiceProcess;

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
                var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "\\dictionary";
                var dictionary = new RadiusDictionary(path);
                var port = Convert.ToInt32(CloudConfigurationManager.GetSetting("Port"));
                var ipassSecret = CloudConfigurationManager.GetSetting("ipasssecret");
                var mbbSecret = CloudConfigurationManager.GetSetting("mbbsecret");
                var mbbNewSecret = CloudConfigurationManager.GetSetting("mbbnewsecret");
                var disconnectSecret = CloudConfigurationManager.GetSetting("disconnectSecret");
                var apiUrl = CloudConfigurationManager.GetSetting("ApiUrl");
                var checkPathOld = CloudConfigurationManager.GetSetting("ipass.checkpathold");
                var checkPathNew = CloudConfigurationManager.GetSetting("ipass.checkpathnew");
                _log.Info("Configuration read");

                var authProxy = new iPassAuthenticationProxy(_contextFactory, checkPathOld, checkPathNew);


                _rsauth = new RadiusServer(new IPEndPoint(IPAddress.Any, port), dictionary);
                _rsacct = new RadiusServer(new IPEndPoint(IPAddress.Any, port + 1), dictionary);    // todo, good grief...

                var ipassPacketHandler = new iPassPacketHandler(_contextFactory, authProxy);
                _rsauth.AddPacketHandler(IPAddress.Parse("127.0.0.1"), ipassSecret, ipassPacketHandler);
                _rsacct.AddPacketHandler(IPAddress.Parse("127.0.0.1"), ipassSecret, ipassPacketHandler);

                var smsgateway = new SMSGatewayTwilio(
                    CloudConfigurationManager.GetSetting("twilio.deliveryreporturl"),
                    CloudConfigurationManager.GetSetting("twilio.accountsid"),
                    CloudConfigurationManager.GetSetting("twilio.authtoken"));

                var networkProvider = new NetworkProvider(_contextFactory);
                var networkApiClient = new NetworkApiClient(_contextFactory, new WebClientFactory(), networkProvider, apiUrl);
                var networkIdProvider = new NetworkIdProvider(new DateTimeProvider(), networkApiClient);
                var welcomeSender = new WelcomeSender(_contextFactory, smsgateway);
                var disconnector = new RadiusDisconnector(_contextFactory, disconnectSecret);
                var mbbPacketHandler = new MobileDataPacketHandler(_contextFactory, networkIdProvider, welcomeSender, disconnector);
                var mbbPacketHandlerV2 = new MobileDataPacketHandlerV2(_contextFactory, welcomeSender, disconnector);

                _rsauth.AddPacketHandler(IPAddress.Parse("10.50.0.253"), mbbSecret, mbbPacketHandler);
                _rsauth.AddPacketHandler(IPAddress.Parse("10.50.0.254"), mbbSecret, mbbPacketHandler);

                _rsacct.AddPacketHandler(IPAddress.Parse("10.50.0.253"), mbbSecret, mbbPacketHandler);
                _rsacct.AddPacketHandler(IPAddress.Parse("10.50.0.254"), mbbSecret, mbbPacketHandler);

                // todo refactor this
                _rsauth.AddPacketHandler(IPAddress.Parse("10.239.24.6"), mbbNewSecret, mbbPacketHandlerV2);
                _rsauth.AddPacketHandler(IPAddress.Parse("10.239.24.7"), mbbNewSecret, mbbPacketHandlerV2);
                _rsauth.AddPacketHandler(IPAddress.Parse("10.239.24.8"), mbbNewSecret, mbbPacketHandlerV2);

                _rsacct.AddPacketHandler(IPAddress.Parse("10.239.24.6"), mbbNewSecret, mbbPacketHandlerV2);
                _rsacct.AddPacketHandler(IPAddress.Parse("10.239.24.7"), mbbNewSecret, mbbPacketHandlerV2);
                _rsacct.AddPacketHandler(IPAddress.Parse("10.239.24.8"), mbbNewSecret, mbbPacketHandlerV2);


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
