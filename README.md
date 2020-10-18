<img src="shadowsocks-csharp/Resources/ssw128.png" alt="[logo]" width="48"/> Shadowsocks for Windows
=======================

[![Build Status]][Appveyor]

[中文说明]

## Features

1. System proxy configuration
2. PAC mode and global mode
3. [GeoSite] and user rules
4. Supports HTTP proxy
5. Supports server auto switching
6. Supports UDP relay (see Usage)
7. Supports plugins

## Downloads

Download the latest release from [release page].

## Requirements

Microsoft [.NET Framework 4.7.2] or higher, Microsoft [Visual C++ 2015 Redistributable] (x86) .

## Basics

1. Find Shadowsocks icon in the notification tray
2. You can add multiple servers in servers menu
3. Select `Enable System Proxy` menu to enable system proxy. Please disable other
proxy addons in your browser, or set them to use system proxy
4. You can also configure your browser proxy manually if you don't want to enable
system proxy. Set Socks5 or HTTP proxy to 127.0.0.1:1080. You can change this
port in `Servers -> Edit Servers`

## PAC

- The PAC rules are generated from the geosite database in [v2fly/domain-list-community](https://github.com/v2fly/domain-list-community).
- Generation modes: whitelist mode and blacklist mode.
- Domain groups: `geositeDirectGroups` and `geositeProxiedGroups`.
    - `geositeDirectGroups` is initialized with `cn` and `geolocation-!cn@cn`.
    - `geositeProxiedGroups` is initialized with `geolocation-!cn`.
- To switch between different modes, modify the `geositePreferDirect` property in `gui-config.json`
    - When `geositePreferDirect` is false (default), PAC works in whitelist mode. Exception rules are generated from `geositeDirectGroups`. Unmatched domains goes through the proxy.
    - When `geositePreferDirect` is true, PAC works in blacklist mode. Blocking rules are generated from `geositeProxiedGroups`. Exception rules are generated from `geositeDirectGroups`. Unmatched domains are connected to directly.
- Starting from 4.3.0.0, shadowsocks-windows defaults to whitelist mode with Chinese domains excluded from connecting via the proxy.
- The new default values make sure that:
    - When in whitelist mode, Chinese domains, including non-Chinese companies' Chinese CDNs, are connected to directly.
    - When in blacklist mode, only non-Chinese domains goes through the proxy. Chinese domains, as well as non-Chinese companies' Chinese CDNs, are connected to directly.

### User-defined rules

- To define your own PAC rules, it's recommended to use the `user-rule.txt` file.
- You can also modify `pac.txt` directly. But your modifications won't persist after updating geosite from the upstream.

For Windows10 Store and related applications, please execute the following command under Admin privilege:
```
netsh winhttp import proxy source=ie
```

## Server Auto Switching

1. Load balance: choosing server randomly
2. High availability: choosing the best server (low latency and packet loss)
3. Choose By Total Package Loss: ping and choose. Please also enable
   `Availability Statistics` in the menu if you want to use this
4. Write your own strategy by implement IStrategy interface and send us a pull request!

## UDP

For UDP, you need to use SocksCap or ProxyCap to force programs you want
to be proxied to tunnel over Shadowsocks

## Multiple Instances

If you want to manage multiple servers using other tools like SwitchyOmega,
you can start multiple Shadowsocks instances. To avoid configuration conflicts,
copy Shadowsocks to a new directory and choose a different local port.

## Plugins

If you would like to connect to server via a plugin, please set the plugin's
path (relative or absolute) on Edit Servers form.
_Note_: Forward Proxy will not be used while a plugin is enabled.

Details:
[Working with non SIP003 standard Plugin].

## Global hotkeys

Hotkeys could be registered automatically on startup.
If you are using multiple instances of Shadowsocks,
you must set different key combination for each instance.

### How to input?

1. Put focus in the corresponding textbox.
2. Press the key combination that you want to use.
3. Release all keys when you think it is ready.
4. Your input appears in the textbox.

### How to change?

1. Put focus in the corresponding textbox.
2. Press BackSpace key to clear content.
3. Re-input new key combination.

### How to deactivate?

1. Clear content in the textbox that you want to deactivate,
if you want to deactivate all, please clear all textboxes.
2. Press OK button to confirm.

### Meaning of label color

- Green: This key combination is not occupied by other programs and register successfully.
- Yellow: This key combination is occupied by other programs and you have to change to another one.
- Transparent without color: The initial status.

## Server Configuration

Please visit [Servers] for more information.

## Experimental

[Experimental Features]

## Development

1. [Visual Studio 2019] & [.NET Framework 4.7.2 Developer Pack] are required.
2. It is recommended to share your idea on the Issue Board before you start to work,
especially for feature development.

## License

[GPLv3]

## Open Source Components / Libraries

```
Caseless.Fody (MIT)              https://github.com/Fody/Caseless
Costura.Fody (MIT)               https://github.com/Fody/Costura
Fody (MIT)                       https://github.com/Fody/Fody
GlobalHotKey (GPLv3)             https://github.com/kirmir/GlobalHotKey
MdXaml (MIT)                     https://github.com/whistyun/MdXaml
Newtonsoft.Json (MIT)            https://www.newtonsoft.com/json
ReactiveUI.WPF (MIT)             https://github.com/reactiveui/ReactiveUI
ReactiveUI.Events.WPF (MIT)      https://github.com/reactiveui/ReactiveUI
ReactiveUI.Fody (MIT)            https://github.com/reactiveui/ReactiveUI
ReactiveUI.Validation (MIT)      https://github.com/reactiveui/ReactiveUI.Validation
WPFLocalizationExtension (MS-PL) https://github.com/XAMLMarkupExtensions/WPFLocalizationExtension/
ZXing.Net (Apache 2.0)           https://github.com/micjahn/ZXing.Net

libsscrypto (GPLv2)    https://github.com/shadowsocks/libsscrypto
Privoxy (GPLv2)        https://www.privoxy.org
Sysproxy ()            https://github.com/Noisyfox/sysproxy
```



[Appveyor]:     https://ci.appveyor.com/project/celeron533/shadowsocks-windows
[Build Status]: https://ci.appveyor.com/api/projects/status/tfw57q6eecippsl5/branch/master?svg=true
[release page]: https://github.com/shadowsocks/shadowsocks-csharp/releases
[GeoSite]:      https://github.com/v2fly/domain-list-community
[Servers]:      https://github.com/shadowsocks/shadowsocks/wiki/Ports-and-Clients#linux--server-side
[中文说明]:     https://github.com/shadowsocks/shadowsocks-windows/wiki/Shadowsocks-Windows-%E4%BD%BF%E7%94%A8%E8%AF%B4%E6%98%8E
[Visual Studio 2017]:   https://www.visualstudio.com/downloads/
[.NET Framework 4.7.2]: https://dotnet.microsoft.com/download/dotnet-framework/net472
[.NET Framework 4.7.2 Developer Pack]: https://dotnet.microsoft.com/download/dotnet-framework/net472
[Visual C++ 2015 Redistributable]:     https://www.microsoft.com/en-us/download/details.aspx?id=53840
[GPLv3]:        https://github.com/shadowsocks/shadowsocks-windows/blob/master/LICENSE.txt
[Working with non SIP003 standard Plugin]: https://github.com/shadowsocks/shadowsocks-windows/wiki/Working-with-non-SIP003-standard-Plugin
[Experimental Features]: https://github.com/shadowsocks/shadowsocks-windows/wiki/Experimental