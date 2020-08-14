using CommandLine;

namespace Shadowsocks
{
    public class CommandLineOption
    {
        [Option("open-url",Required = false,HelpText = "Add an ss:// URL")]
        public string OpenUrl { get; set; }
    }
}