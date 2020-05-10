using NLog;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace Shadowsocks.Controller
{
    static class GeositeUpdater
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string DatabasePath = Utils.GetTempPath("dlc.dat");

        public static readonly GeositeList List;

        public static readonly Dictionary<string, List<DomainObject>> Geosites = new Dictionary<string, List<DomainObject>>();

        static GeositeUpdater()
        {
            if (!File.Exists(DatabasePath))
            {
                File.WriteAllBytes(DatabasePath, Resources.dlc_dat);
            }

            List = GeositeList.Parser.ParseFrom(File.ReadAllBytes(DatabasePath));


            foreach (var item in List.Entry)
            {
                Geosites[item.CountryCode] = item.Domain.ToList();
            }
        }
    }
}
