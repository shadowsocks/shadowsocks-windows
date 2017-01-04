ShadowsocksR for Windows
=======================

[![Build Status]][Appveyor]

#### Download

Download a [latest release].

For >= Windows 8 or with .Net 4.0, using ShadowsocksR-dotnet4.0.exe.

For <= Windows 7 or with .Net 2.0, using ShadowsocksR-dotnet2.0.exe.

#### Usage

1. Find ShadowsocksR icon in the notification tray
2. You can add multiple servers in servers menu
3. Select Enable System Proxy menu to enable system proxy. Please disable other
proxy addons in your browser, or set them to use system proxy
4. You can also configure your browser proxy manually if you don't want to enable
system proxy. Set Socks5 or HTTP proxy to 127.0.0.1:1080. You can change this
port in Global settings
5. You can change PAC rules by editing the PAC file. When you save the PAC file
with any editor, ShadowsocksR will notify browsers about the change automatically
6. You can also update the PAC file from GFWList. Note your modifications to the PAC
file will be lost. However you can put your rules in the user rule file for GFWList.
Don't forget to update from GFWList again after you've edited the user rule
7. For UDP, you need to use SocksCap or ProxyCap to force programs you want
to proxy to tunnel over ShadowsocksR

### Develop

Visual Studio Express 2012 is recommended.

#### License

GPLv3

Copyright © BreakWa11 2017. Fork from Shadowsocks by clowwindy

[Appveyor]:       https://ci.appveyor.com/project/breakwa11/shadowsocksr-csharp
[Build Status]:   https://ci.appveyor.com/api/projects/status/itcxnad1y95gf2x5/branch/master?svg=true
[latest release]: https://github.com/shadowsocksr/shadowsocksr-csharp/releases
