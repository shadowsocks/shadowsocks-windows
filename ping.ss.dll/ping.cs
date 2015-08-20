using Shadowsocks.Controller;

namespace ping.ss
{
    public static class ssping
    {
        public static string PluginName => "Ping测试";

        public static string Author => "TsungKang";

        public static void Invoke(ShadowsocksController sc)
        {
            var fm = new frmMain(sc);
            fm.ShowDialog();
        }
    }
}
