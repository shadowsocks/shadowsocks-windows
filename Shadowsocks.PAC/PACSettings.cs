using System.Collections.Generic;

namespace Shadowsocks.PAC;

/// <summary>
/// Settings used for PAC.
/// </summary>
public class PacSettings
{
    /// <summary>
    /// Controls whether direct connection is used for
    /// hostnames not matched by blocking rules
    /// or matched by exception rules.
    /// Defaults to false, or whitelist mode,
    /// where hostnames matching the above conditions
    /// are connected to via proxy.
    /// Enable it to use blacklist mode.
    /// </summary>
    public bool PacDefaultToDirect { get; set; } = false;

    /// <summary>
    /// Controls whether the PAC server uses a secret
    /// to protect access to the PAC URL.
    /// Defaults to true.
    /// </summary>
    public bool PacServerEnableSecret { get; set; } = true;

    /// <summary>
    /// Controls whether `pac.txt` should be regenerated
    /// when shadowsocks-windows is updated.
    /// Defaults to true, so new changes can be applied.
    /// Change it to false if you want to manage `pac.txt`
    /// yourself.
    /// </summary>
    public bool RegeneratePacOnVersionUpdate { get; set; } = true;

    /// <summary>
    /// Specifies a custom PAC URL.
    /// Leave empty to use local PAC.
    /// </summary>
    public string CustomPacUrl { get; set; } = "";

    /// <summary>
    /// Specifies a custom GeoSite database URL.
    /// Leave empty to use the default source.
    /// </summary>
    public string CustomGeoSiteUrl { get; set; } = "";

    /// <summary>
    /// Specifies the custom GeoSite database's corresponding SHA256 checksum download URL.
    /// Leave empty to disable checksum verification for your custom GeoSite database.
    /// </summary>
    public string CustomGeoSiteSha256SumUrl { get; set; } = "";

    /// <summary>
    /// A list of GeoSite groups
    /// that we use direct connection for.
    /// </summary>
    public List<string> GeoSiteDirectGroups { get; set; } =
    [
        "private",
        "cn",
        "geolocation-!cn@cn",
    ];

    /// <summary>
    /// A list of GeoSite groups
    /// that we always connect to via proxy.
    /// </summary>
    public List<string> GeoSiteProxiedGroups { get; set; } = ["geolocation-!cn"];
}