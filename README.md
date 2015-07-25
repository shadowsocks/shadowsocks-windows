Shadowsocks for Windows
=======================

[![Build Status]][Appveyor]

#### Features

1. System proxy configuration
2. PAC mode and global mode
3. GFWList and user rules
4. Supports HTTP proxy
5. Supports server auto switching
6. Supports UDP relay (see Usage)

#### Download

Download a [latest release].

For >= Windows 8 or with .Net 4.0, download Shadowsocks-win-dotnet4.0-x.x.x.zip.

For <= Windows 7 or with .Net 2.0, download Shadowsocks-win-x.x.x.zip.

#### Basic

1. Find Shadowsocks icon in the notification tray
2. You can add multiple servers in servers menu
3. Select Enable System Proxy menu to enable system proxy. Please disable other
proxy addons in your browser, or set them to use system proxy
4. You can also configure your browser proxy manually if you don't want to enable
system proxy. Set Socks5 or HTTP proxy to 127.0.0.1:1080. You can change this
port in Server -> Edit Servers

#### PAC

1. You can change PAC rules by editing the PAC file. When you save the PAC file
with any editor, Shadowsocks will notify browsers about the change automatically
2. You can also update PAC file from GFWList (maintained by 3rd party)
3. You can also use online PAC URL

#### Server Auto Switching

1. Load balance: choosing server randomly
2. High availability: choosing the best server (low latency and packet loss)
3. Write your own strategy by implement IStrategy interface and send us a pull request!

#### UDP

For UDP, you need to use SocksCap or ProxyCap to force programs you want
to proxy to tunnel over Shadowsocks

#### License

GPLv3


[Appveyor]:       https://ci.appveyor.com/project/clowwindy/shadowsocks-csharp
[Build Status]:   https://ci.appveyor.com/api/projects/status/gknc8l1lxy423ehv/branch/master
[latest release]: https://github.com/shadowsocks/shadowsocks-csharp/releases
