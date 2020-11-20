using Shadowsocks.WPF.Models;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shadowsocks.WPF.Services
{
    /// <summary>
    /// The service for updating a group from an SIP008 online configuration source.
    /// </summary>
    public class OnlineConfigService
    {
        private Group _group;
        private HttpClient _httpClient;
        
        public OnlineConfigService(Group group)
        {
            _group = group;
            _httpClient = Locator.Current.GetService<HttpClient>();
        }

        /// <summary>
        /// Updates the group from the configured online configuration source.
        /// </summary>
        /// <returns></returns>
        public async Task Update()
        {
            // Download
            var downloadedGroup = await _httpClient.GetFromJsonAsync<Group>(_group.OnlineConfigSource);
            if (downloadedGroup == null)
                throw new Exception("An error occurred.");
            // Merge downloaded group into existing group
            _group.Version = downloadedGroup.Version;
            _group.BytesUsed = downloadedGroup.BytesUsed;
            _group.BytesRemaining = downloadedGroup.BytesRemaining;
            _group.Servers = downloadedGroup.Servers; // TODO: preserve per-server statistics
        }
    }
}
