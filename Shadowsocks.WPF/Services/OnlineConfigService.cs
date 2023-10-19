using Shadowsocks.WPF.Models;
using Splat;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Shadowsocks.WPF.Services;

/// <summary>
/// The service for updating a group from an SIP008 online configuration source.
/// </summary>
public class OnlineConfigService(Group group)
{
    private readonly HttpClient _httpClient = Locator.Current.GetService<HttpClient>();

    /// <summary>
    /// Updates the group from the configured online configuration source.
    /// </summary>
    /// <returns></returns>
    public async Task Update()
    {
        // Download
        var downloadedGroup = await _httpClient.GetFromJsonAsync<Group>(group.OnlineConfigSource);
        if (downloadedGroup == null)
            throw new Exception("An error occurred.");
        // Merge downloaded group into existing group
        group.Version = downloadedGroup.Version;
        group.BytesUsed = downloadedGroup.BytesUsed;
        group.BytesRemaining = downloadedGroup.BytesRemaining;
        group.Servers = downloadedGroup.Servers; // TODO: preserve per-server statistics
    }
}