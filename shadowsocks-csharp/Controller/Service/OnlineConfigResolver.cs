using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Shadowsocks.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shadowsocks.Controller.Service
{
    public class OnlineConfigResolver
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task<List<Server>> GetOnline(string url, IWebProxy proxy = null)
        {
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler);

            if (proxy != null)
            {
                httpClientHandler.Proxy = proxy;
            }

            try
            {
                string str = await httpClient.GetStringAsync(url);
                var ret = Get(str);
                foreach (var item in ret)
                {
                    item.group = url;
                }
                return ret;
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
                return new List<Server>();
            }
        }

        public static List<Server> Get(string json)
        {
            try
            {
                var t = JToken.Parse(json);
                return SearchJToken(t).ToList();
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
                return new List<Server>();
            }
        }

        private static IEnumerable<Server> SearchJArray(JArray a)
        {
            if (a == null) return Array.Empty<Server>();
            return a.SelectMany(SearchJToken).ToList();
        }

        private static IEnumerable<Server> SearchJObject(JObject o)
        {
            var l = new List<Server>();
            if (o == null) return l;
            if (IsServerObject(o))
                return new List<Server> { o.ToObject<Server>() };

            foreach (var kv in o)
            {
                JToken v = kv.Value;
                l.AddRange(SearchJToken(v));
            }
            return l;
        }

        private static IEnumerable<Server> SearchJToken(JToken t)
        {
            switch (t.Type)
            {
                default:
                    return Array.Empty<Server>();
                case JTokenType.Object:
                    return SearchJObject(t as JObject);
                case JTokenType.Array:
                    return SearchJArray(t as JArray);
            }
        }

        private static bool IsServerObject(JObject o)
        {
            return new[] { "server", "server_port", "password", "method" }.All(i => o.ContainsKey(i));
        }
    }
}
