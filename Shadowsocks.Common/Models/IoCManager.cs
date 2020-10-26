using SimpleInjector;

namespace Shadowsocks.Common.Model
{
    public static class IoCManager
    {
        public static Container Container { get; } = new Container();
    }
}
