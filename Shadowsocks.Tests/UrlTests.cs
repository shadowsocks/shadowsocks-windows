using Shadowsocks.Models;
using System;
using Xunit;

namespace Shadowsocks.Tests
{
    public class UrlTests
    {
        [Theory]
        [InlineData("chacha20-ietf-poly1305", "kf!V!TFzgeNd93GE", "Y2hhY2hhMjAtaWV0Zi1wb2x5MTMwNTprZiFWIVRGemdlTmQ5M0dF")]
        [InlineData("aes-256-gcm", "ymghiR#75TNqpa", "YWVzLTI1Ni1nY206eW1naGlSIzc1VE5xcGE")]
        [InlineData("aes-128-gcm", "tK*sk!9N8@86:UVm", "YWVzLTEyOC1nY206dEsqc2shOU44QDg2OlVWbQ")]
        public void Utilities_Base64Url_Encode(string method, string password, string expectedUserinfoBase64url)
        {
            var userinfoBase64url = Utilities.Base64Url.Encode($"{method}:{password}");

            Assert.Equal(expectedUserinfoBase64url, userinfoBase64url);
        }
 
        [Theory]
        [InlineData("Y2hhY2hhMjAtaWV0Zi1wb2x5MTMwNTo2JW04RDlhTUI1YkElYTQl", "chacha20-ietf-poly1305:6%m8D9aMB5bA%a4%")]
        [InlineData("YWVzLTI1Ni1nY206YnBOZ2sqSjNrYUFZeXhIRQ", "aes-256-gcm:bpNgk*J3kaAYyxHE")]
        [InlineData("YWVzLTEyOC1nY206dkFBbiY4a1I6JGlBRTQ", "aes-128-gcm:vAAn&8kR:$iAE4")]
        public void Utilities_Base64Url_Decode(string userinfoBase64url, string expectedUserinfo)
        {
            var userinfo = Utilities.Base64Url.DecodeToString(userinfoBase64url);

            Assert.Equal(expectedUserinfo, userinfo);
        }

        [Theory]
        [InlineData("aes-256-gcm", "wLhN2STZ", "github.com", 443, "", null, null, "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/")] // domain name
        [InlineData("aes-256-gcm", "wLhN2STZ", "1.1.1.1", 853, "", null, null, "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@1.1.1.1:853/")] // IPv4
        [InlineData("aes-256-gcm", "wLhN2STZ", "2001:db8:85a3::8a2e:370:7334", 8388, "", null, null, "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@[2001:db8:85a3::8a2e:370:7334]:8388/")] // IPv6
        [InlineData("aes-256-gcm", "wLhN2STZ", "github.com", 443, "GitHub", null, null, "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/#GitHub")] // fragment
        [InlineData("aes-256-gcm", "wLhN2STZ", "github.com", 443, "👩‍💻", null, null, "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/#%F0%9F%91%A9%E2%80%8D%F0%9F%92%BB")] // fragment
        [InlineData("aes-256-gcm", "wLhN2STZ", "github.com", 443, "", "v2ray-plugin", null, "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/?plugin=v2ray-plugin")] // plugin
        [InlineData("aes-256-gcm", "wLhN2STZ", "github.com", 443, "", null, "server;tls;host=github.com", "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/")] // pluginOpts
        [InlineData("aes-256-gcm", "wLhN2STZ", "github.com", 443, "", "v2ray-plugin", "server;tls;host=github.com", "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/?plugin=v2ray-plugin%3Bserver%3Btls%3Bhost%3Dgithub.com")] // plugin + pluginOpts
        [InlineData("aes-256-gcm", "wLhN2STZ", "github.com", 443, "GitHub", "v2ray-plugin", "server;tls;host=github.com", "ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/?plugin=v2ray-plugin%3Bserver%3Btls%3Bhost%3Dgithub.com#GitHub")] // fragment + plugin + pluginOpts
        public void Server_ToUrl(string method, string password, string host, int port, string fragment, string? plugin, string? pluginOpts, string expectedSSUri)
        {
            var server = new Server()
            {
                Password = password,
                Method = method,
                Host = host,
                Port = port,
                Name = fragment,
                Plugin = plugin,
                PluginOpts = pluginOpts,
            };

            var ssUriString = server.ToUrl().AbsoluteUri;

            Assert.Equal(expectedSSUri, ssUriString);
        }

        [Theory]
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/", true, "aes-256-gcm", "wLhN2STZ", "github.com", 443, "", null, null)] // domain name
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@1.1.1.1:853/", true, "aes-256-gcm", "wLhN2STZ", "1.1.1.1", 853, "", null, null)] // IPv4
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@[2001:db8:85a3::8a2e:370:7334]:8388/", true, "aes-256-gcm", "wLhN2STZ", "2001:db8:85a3::8a2e:370:7334", 8388, "", null, null)] // IPv6
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/#GitHub", true, "aes-256-gcm", "wLhN2STZ", "github.com", 443, "GitHub", null, null)] // fragment
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/#%F0%9F%91%A9%E2%80%8D%F0%9F%92%BB", true, "aes-256-gcm", "wLhN2STZ", "github.com", 443, "👩‍💻", null, null)] // fragment
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/?plugin=v2ray-plugin", true, "aes-256-gcm", "wLhN2STZ", "github.com", 443, "", "v2ray-plugin", null)] // plugin
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/?plugin=v2ray-plugin%3Bserver%3Btls%3Bhost%3Dgithub.com", true, "aes-256-gcm", "wLhN2STZ", "github.com", 443, "", "v2ray-plugin", "server;tls;host=github.com")] // plugin + pluginOpts
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFo@github.com:443/?plugin=v2ray-plugin%3Bserver%3Btls%3Bhost%3Dgithub.com#GitHub", true, "aes-256-gcm", "wLhN2STZ", "github.com", 443, "GitHub", "v2ray-plugin", "server;tls;host=github.com")] // fragment + plugin + pluginOpts
        [InlineData("ss://Y2hhY2hhMjAtaWV0Zi1wb2x5MTMwNTo2JW04RDlhTUI1YkElYTQl@github.com:443/", true, "chacha20-ietf-poly1305", "6%m8D9aMB5bA%a4%", "github.com", 443, "", null, null)] // userinfo parsing
        [InlineData("ss://YWVzLTI1Ni1nY206YnBOZ2sqSjNrYUFZeXhIRQ@github.com:443/", true, "aes-256-gcm", "bpNgk*J3kaAYyxHE", "github.com", 443, "", null, null)] // userinfo parsing
        [InlineData("ss://YWVzLTEyOC1nY206dkFBbiY4a1I6JGlBRTQ@github.com:443/", true, "aes-128-gcm", "vAAn&8kR:$iAE4", "github.com", 443, "", null, null)] // userinfo parsing
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFpAZ2l0aHViLmNvbTo0NDM", false, "", "", "", 0, "", null, null)] // unsupported legacy URL
        [InlineData("ss://YWVzLTI1Ni1nY206d0xoTjJTVFpAZ2l0aHViLmNvbTo0NDM#some-legacy-url", false, "", "", "", 0, "", null, null)] // unsupported legacy URL with fragment
        [InlineData("https://github.com/", false, "", "", "", 0, "", null, null)] // non-Shadowsocks URL
        public void Server_TryParse(string ssUrl, bool expectedResult, string expectedMethod, string expectedPassword, string expectedHost, int expectedPort, string expectedFragment, string? expectedPlugin, string? expectedPluginOpts)
        {
            var result = Server.TryParse(ssUrl, out var server);

            Assert.Equal(expectedResult, result);
            if (result)
            {
                Assert.Equal(expectedPassword, server.Password);
                Assert.Equal(expectedMethod, server.Method);
                Assert.Equal(expectedHost, server.Host);
                Assert.Equal(expectedPort, server.Port);
                Assert.Equal(expectedFragment, server.Name);
                Assert.Equal(expectedPlugin, server.Plugin);
                Assert.Equal(expectedPluginOpts, server.PluginOpts);
            }
        }
   }
}
