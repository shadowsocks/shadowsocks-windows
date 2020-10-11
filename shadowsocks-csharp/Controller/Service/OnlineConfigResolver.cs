using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shadowsocks.Model;

namespace Shadowsocks.Controller.Service
{
    public class OnlineConfigResolver
    {
        public static async Task<List<Server>> GetOnline(string url, string userAgentString, IWebProxy proxy = null)
        {
            var httpClientHandler = new HttpClientHandler()
            {
                Proxy = proxy
            };
            var httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            if (!string.IsNullOrWhiteSpace(userAgentString))
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgentString);

            string server_json = await httpClient.GetStringAsync(url);

            var servers = server_json.GetServers();

            foreach (var server in servers)
            {
                server.group = url;
            }

            return servers.ToList();
        }
    }

    internal static class OnlineConfigResolverEx
    {
        private static readonly string[] BASIC_FORMAT = new[] { "server", "server_port", "password", "method" };

        private static readonly IEnumerable<Server> EMPTY_SERVERS = Array.Empty<Server>();

        internal static IEnumerable<Server> GetServers(this string json) =>
            JToken.Parse(json).SearchJToken().AsEnumerable();

        private static IEnumerable<Server> SearchJArray(JArray array) =>
            array == null ? EMPTY_SERVERS : array.SelectMany(SearchJToken).ToList();

        private static IEnumerable<Server> SearchJObject(JObject obj)
        {
            if (obj == null)
                return EMPTY_SERVERS;

            if (BASIC_FORMAT.All(field => obj.ContainsKey(field)))
                return new[] { obj.ToObject<Server>() };

            var servers = new List<Server>();
            foreach (var kv in obj)
            {
                var token = kv.Value;
                servers.AddRange(SearchJToken(token));
            }
            return servers;
        }

        private static IEnumerable<Server> SearchJToken(this JToken token)
        {
            switch (token.Type)
            {
                default:
                    return Array.Empty<Server>();
                case JTokenType.Object:
                    return SearchJObject(token as JObject);
                case JTokenType.Array:
                    return SearchJArray(token as JArray);
            }
        }
    }
}
