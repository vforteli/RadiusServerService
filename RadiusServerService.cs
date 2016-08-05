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
        private RadiusServer _rs;
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
                _contextFactory = new FlexinetsEntitiesFactory(CloudConfigurationManager.GetSetting("SQLConnectionString"));

                var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\dictionary";  // todo hurgh
                var dictionary = new RadiusDictionary(path);

                var port = Convert.ToInt32(CloudConfigurationManager.GetSetting("Port"));
                var secret = CloudConfigurationManager.GetSetting("secret");
                _log.Info("Configuration read");
                _rs = new RadiusServer(new IPEndPoint(IPAddress.Any, port), dictionary);
                _rs.AddPacketHandler(IPAddress.Parse("127.0.0.1"), secret, new iPassPacketHandler(_contextFactory));
                _rs.Start();
            }
            catch (Exception ex)
            {
                _log.Fatal("Failed to start service", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            _rs.Stop();
            _rs.Dispose();
        }
    }
}
