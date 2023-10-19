using System.Reflection;
using WPFLocalizeExtension.Extensions;

namespace Shadowsocks.WPF.Localization;

public class LocalizationProvider
{
    private static readonly string _callingAssemblyName = Assembly.GetCallingAssembly().GetName().Name ?? "Shadowsocks.WPF";

    public static T GetLocalizedValue<T>(string key) => LocExtension.GetLocalizedValue<T>($"{_callingAssemblyName}:Strings:{key}");
}