using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray;

public class ApiObject
{
    /// <summary>
    /// Gets or sets the outbound tag for the API.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of API services to enable.
    /// </summary>
    public List<string> Services { get; set; } = [];

    /// <summary>
    /// Gets the default API object.
    /// </summary>
    public static ApiObject Default => new()
    {
        Tag = "api",
        Services =
        [
            "HandlerService",
            "LoggerService",
            "StatsService",
        ],
    };
}