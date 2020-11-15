<img src="Shadowsocks.WPF/Resources/ssw128.png" alt="[logo]" width="48"/> Shadowsocks for Windows
=======================

[![Build Status](https://ci.appveyor.com/api/projects/status/tfw57q6eecippsl5/branch/master?svg=true)](https://ci.appveyor.com/project/celeron533/shadowsocks-windows)

## Features

- Connect to Shadowsocks servers.
- Automatically set system proxy.
- SIP002 URL scheme.
- SIP003 plugins.
- SIP008 online configuration delivery.

## Downloads

Download from [releases](https://github.com/shadowsocks/shadowsocks-windows/releases).

## Usage

- 🚀

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

## Development

- IDE: Visual Studio 2019
- Language: C# 9.0
- SDK: .NET 5

### Build

1. Clone the repository recursively.
```bash
$ git clone --recursive https://github.com/shadowsocks/shadowsocks-windows.git
```
2. Open the repository in VS2019, switch to the _Release_ configuration, and build the solution.

### Contribute

`PR welcome`

You can use the [Source Browser](https://ss-windows.cube64128.xyz/) to review code online.

## License

Shadowsocks-windows is licensed under the [GPLv3](LICENSE.txt) license.

```
BouncyCastle.NetCore (MIT)       https://github.com/chrishaly/bc-csharp
Caseless.Fody (MIT)              https://github.com/Fody/Caseless
Costura.Fody (MIT)               https://github.com/Fody/Costura
Fody (MIT)                       https://github.com/Fody/Fody
GlobalHotKey (GPLv3)             https://github.com/kirmir/GlobalHotKey
MdXaml (MIT)                     https://github.com/whistyun/MdXaml
Newtonsoft.Json (MIT)            https://www.newtonsoft.com/json
Privoxy (GPLv2)                  https://www.privoxy.org
ReactiveUI.WPF (MIT)             https://github.com/reactiveui/ReactiveUI
ReactiveUI.Events.WPF (MIT)      https://github.com/reactiveui/ReactiveUI
ReactiveUI.Fody (MIT)            https://github.com/reactiveui/ReactiveUI
ReactiveUI.Validation (MIT)      https://github.com/reactiveui/ReactiveUI.Validation
WPFLocalizationExtension (MS-PL) https://github.com/XAMLMarkupExtensions/WPFLocalizationExtension/
ZXing.Net (Apache 2.0)           https://github.com/micjahn/ZXing.Net
```
