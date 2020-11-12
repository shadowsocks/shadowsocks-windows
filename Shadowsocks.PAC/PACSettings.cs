using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.PAC
{
    /// <summary>
    /// Settings used for PAC.
    /// </summary>
    public class PACSettings
    {
        public PACSettings()
        {
            PACDefaultToDirect = false;
            PACServerEnableSecret = true;
            RegeneratePacOnVersionUpdate = true;
            CustomPACUrl = "";
            CustomGeositeUrl = "";
            CustomGeositeSha256SumUrl = "";
            GeositeDirectGroups = new List<string>()
            {
                "private",
                "cn",
                "geolocation-!cn@cn",
            };
            GeositeProxiedGroups = new List<string>()
            {
                "geolocation-!cn",
            };
        }

        /// <summary>
        /// Controls whether direct connection is used for
        /// hostnames not matched by blocking rules
        /// or matched by exception rules.
        /// Defaults to false, or whitelist mode,
        /// where hostnames matching the above conditions
        /// are connected to via proxy.
        /// Enable it to use blacklist mode.
        /// </summary>
        public bool PACDefaultToDirect { get; set; }

        /// <summary>
        /// Controls whether the PAC server uses a secret
        /// to protect access to the PAC URL.
        /// Defaults to true.
        /// </summary>
        public bool PACServerEnableSecret { get; set; }

        /// <summary>
        /// Controls whether `pac.txt` should be regenerated
        /// when shadowsocks-windows is updated.
        /// Defaults to true, so new changes can be applied.
        /// Change it to false if you want to manage `pac.txt`
        /// yourself.
        /// </summary>
        public bool RegeneratePacOnVersionUpdate { get; set; }

        /// <summary>
        /// Specifies a custom PAC URL.
        /// Leave empty to use local PAC.
        /// </summary>
        public string CustomPACUrl { get; set; }

        /// <summary>
        /// Specifies a custom Geosite database URL.
        /// Leave empty to use the default source.
        /// </summary>
        public string CustomGeositeUrl { get; set; }

        /// <summary>
        /// Specifies the custom Geosite database's corresponding SHA256 checksum download URL.
        /// Leave empty to disable checksum verification for your custom Geosite database.
        /// </summary>
        public string CustomGeositeSha256SumUrl { get; set; }

        /// <summary>
        /// A list of Geosite groups
        /// that we use direct connection for.
        /// </summary>
        public List<string> GeositeDirectGroups { get; set; }

        /// <summary>
        /// A list of Geosite groups
        /// that we always connect to via proxy.
        /// </summary>
        public List<string> GeositeProxiedGroups { get; set; }
    }
}
