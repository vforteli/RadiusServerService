using FlexinetsDBEF;
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
        private readonly FlexinetsEntitiesFactory _contextFactory;
        private RadiusServer _rs;
        private const String _sharedsecret = "harald";
        private const int port = 1812;

        public RadiusServerService()
        {
            InitializeComponent();

            log4net.Config.XmlConfigurator.Configure();
            _contextFactory = new FlexinetsEntitiesFactory(CloudConfigurationManager.GetSetting("SQLConnectionString"));
        }

        protected override void OnStart(string[] args)
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\dictionary";  // todo hurgh
            var dictionary = new RadiusDictionary(path);

            _rs = new RadiusServer(new IPEndPoint(IPAddress.Any, port), dictionary);
            _rs.AddPacketHandler(IPAddress.Parse("127.0.0.1"), _sharedsecret, new iPassPacketHandler(_contextFactory));
            _rs.Start();
        }

        protected override void OnStop()
        {
            _rs.Stop();
            _rs.Dispose();
        }
    }
}
