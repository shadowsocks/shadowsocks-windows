using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaxMind.GeoIP2;

namespace Shadowsocks.GeoIP
{
    class GeoIP
    {
        public string GetCityCode(string Hostname)
        {

            string Country;
            try
            {
                var databaseReader = new DatabaseReader("GeoLite2-Country.mmdb");

                if (System.Text.RegularExpressions.Regex.IsMatch(Hostname, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$"))
                {
                    Country = databaseReader.Country(Hostname).Country.IsoCode;
                }
                else
                {
                    var DnsResult = System.Net.Dns.GetHostEntry(Hostname).AddressList[0].ToString();

                    if (DnsResult != null)
                    {
                        Country = databaseReader.Country(DnsResult).Country.IsoCode;
                    }
                    else
                    {
                        Country = "";
                    }
                }
            }
            catch (Exception)
            {
                Country = "";
            }

            return Country == null ? "" : Country;
        }
    }
}
