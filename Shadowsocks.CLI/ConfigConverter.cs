using Shadowsocks.Interop.Utils;
using Shadowsocks.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.CLI;

public class ConfigConverter(bool prefixGroupName = false)
{
    /// <summary>
    /// Gets or sets whether to prefix group name to server names.
    /// </summary>
    public bool PrefixGroupName { get; set; } = prefixGroupName;

    /// <summary>
    /// Gets or sets the list of servers that are not in any groups.
    /// </summary>
    public List<Server> Servers { get; set; } = [];

    /// <summary>
    /// Collects servers from ss:// links or SIP008 delivery links.
    /// </summary>
    /// <param name="uris">URLs to collect servers from.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FromUrls(IEnumerable<Uri> uris, CancellationToken cancellationToken = default)
    {
        var sip008Links = new List<Uri>();

        foreach (var uri in uris)
        {
            switch (uri.Scheme)
            {
                case "ss":
                {
                    if (Server.TryParse(uri, out var server))
                        Servers.Add(server);
                    break;
                }

                case "https":
                    sip008Links.Add(uri);
                    break;
            }
        }

        if (sip008Links.Count > 0)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30.0)
            };
            var tasks = sip008Links.Select(async x => await httpClient.GetFromJsonAsync<Group>(x, JsonHelper.SnakeCaseJsonDeserializerOptions, cancellationToken))
                .ToList();
            while (tasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(tasks);
                var group = await finishedTask;
                if (group != null)
                    Servers.AddRange(group.Servers);
                tasks.Remove(finishedTask);
            }
        }
    }

    /// <summary>
    /// Collects servers from SIP008 JSON files.
    /// </summary>
    /// <param name="paths">JSON file paths.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the read operation.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    public async Task FromSip008Json(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        foreach (var path in paths)
        {
            await using var jsonFile = new FileStream(path, FileMode.Open);
            var group = await JsonSerializer.DeserializeAsync<Group>(jsonFile, JsonHelper.SnakeCaseJsonDeserializerOptions, cancellationToken);
            if (group is null) continue;
            if (PrefixGroupName && !string.IsNullOrEmpty(group.Name))
                group.Servers.ForEach(x => x.Name = $"{group.Name} - {x.Name}");
            Servers.AddRange(group.Servers);
        }
    }

    /// <summary>
    /// Collects servers from outbounds in V2Ray JSON files.
    /// </summary>
    /// <param name="paths">JSON file paths.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the read operation.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    public async Task FromV2rayJson(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        foreach (var path in paths)
        {
            using var jsonFile = new FileStream(path, FileMode.Open);
            var v2rayConfig = await JsonSerializer.DeserializeAsync<Interop.V2Ray.Config>(jsonFile, JsonHelper.CamelCaseJsonDeserializerOptions, cancellationToken);
            if (v2rayConfig?.Outbounds != null)
            {
                foreach (var outbound in v2rayConfig.Outbounds)
                {
                    if (outbound.Protocol == "shadowsocks"
                        && outbound.Settings is JsonElement jsonElement)
                    {
                        var jsonText = jsonElement.GetRawText();
                        var ssConfig = JsonSerializer.Deserialize<Interop.V2Ray.Protocols.Shadowsocks.OutboundConfigurationObject>(jsonText, JsonHelper.CamelCaseJsonDeserializerOptions);
                        if (ssConfig != null)
                            foreach (var ssServer in ssConfig.Servers)
                            {
                                var server = new Server
                                {
                                    Name = outbound.Tag,
                                    Host = ssServer.Address,
                                    Port = ssServer.Port,
                                    Method = ssServer.Method,
                                    Password = ssServer.Password
                                };
                                Servers.Add(server);
                            }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Converts saved servers to ss:// URLs.
    /// </summary>
    /// <returns>A list of ss:// URLs.</returns>
    public List<Uri> ToUrls()
    {
        var urls = new List<Uri>();

        foreach (var server in Servers)
            urls.Add(server.ToUrl());

        return urls;
    }

    /// <summary>
    /// Converts saved servers to SIP008 JSON.
    /// </summary>
    /// <param name="path">JSON file path.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public Task ToSip008Json(string path, CancellationToken cancellationToken = default)
    {
        var group = new Group();

        group.Servers.AddRange(Servers);

        var fullPath = Path.GetFullPath(path);
        var directoryPath = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("Invalid path", nameof(path));
        Directory.CreateDirectory(directoryPath);
        using var jsonFile = new FileStream(fullPath, FileMode.Create);
        return JsonSerializer.SerializeAsync(jsonFile, group, JsonHelper.SnakeCaseJsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Converts saved servers to V2Ray outbounds.
    /// </summary>
    /// <param name="path">JSON file path.</param>
    /// <param name="prefixGroupName">Whether to prefix group name to server names.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public Task ToV2RayJson(string path, CancellationToken cancellationToken = default)
    {
        var v2RayConfig = new Interop.V2Ray.Config
        {
            Outbounds = []
        };

        foreach (var ssOutbound in Servers.Select(Interop.V2Ray.OutboundObject.GetShadowsocks))
        {
            v2RayConfig.Outbounds.Add(ssOutbound);
        }

        // enforce outbound tag uniqueness
        var serversWithDuplicateTags = v2RayConfig.Outbounds.GroupBy(x => x.Tag)
            .Where(x => x.Count() > 1);
        foreach (var serversWithSameTag in serversWithDuplicateTags)
        {
            var duplicates = serversWithSameTag.ToList();
            for (var i = 0; i < duplicates.Count; i++)
            {
                duplicates[i].Tag = $"{duplicates[i].Tag} {i}";
            }
        }

        var fullPath = Path.GetFullPath(path);
        var directoryPath = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("Invalid path", nameof(path));
        Directory.CreateDirectory(directoryPath);
        using var jsonFile = new FileStream(fullPath, FileMode.Create);
        return JsonSerializer.SerializeAsync(jsonFile, v2RayConfig, JsonHelper.CamelCaseJsonSerializerOptions, cancellationToken);
    }
}