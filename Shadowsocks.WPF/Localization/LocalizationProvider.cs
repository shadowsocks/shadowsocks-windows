using Shadowsocks.Common.Model;

using System.Reflection;

using WPFLocalizeExtension.Extensions;

namespace Shadowsocks.WPF.Localization
{
    public class LocalizationProvider : ILocalizationProvider
    {
        private static readonly string CallingAssemblyName = Assembly.GetCallingAssembly().GetName().Name;

        public T GetLocalizedValue<T>(string key) => LocExtension.GetLocalizedValue<T>($"{CallingAssemblyName}:Strings:{key}");
    }
}
