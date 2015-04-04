Shadowsocks for Windows
=======================

[![Build Status]][Appveyor]

#### Features

1. System proxy configuration
2. Fast profile switching
3. PAC mode and global mode
4. GFWList and user rules
5. Supports HTTP proxy

#### Download

Download a [latest release].

For >= Windows 8 or with .Net 4.0, download Shadowsocks-win-dotnet4.0-x.x.x.zip.

For <= Windows 7 or with .Net 2.0, download Shadowsocks-win-x.x.x.zip.

#### Usage

1. Find Shadowsocks icon in the notification tray
2. You can add multiple servers in servers menu
3. Select Enable System Proxy menu to enable system proxy. Please disable other
proxy addons in your browser, or set them to use system proxy
4. You can also configure your browser proxy manually if you don't want to enable
system proxy. Set Socks5 or HTTP proxy to 127.0.0.1:1080. You can change this
port in Server -> Edit Servers
5. You can change PAC rules by editing the PAC file. When you save the PAC file
with any editor, Shadowsocks will notify browsers about the change automatically
6. You can also update the PAC file from GFWList. Note your modifications to the PAC
file will be lost. However you can put your rules in the user rule file for GFWList.
Don't forget to update from GFWList again after you've edited the user rule

### Develop

Visual Studio Express 2012 is recommended.

#### License

GPLv3


[Appveyor]:       https://ci.appveyor.com/project/clowwindy/shadowsocks-csharp
[Build Status]:   https://ci.appveyor.com/api/projects/status/gknc8l1lxy423ehv/branch/master
[latest release]: https://sourceforge.net/projects/shadowsocksgui/files/dist/
