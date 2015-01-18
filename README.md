Shadowsocks for Windows
=======================

[![Build Status]][Appveyor]

#### Features

1. System proxy configuration
2. Fast profile switching
3. PAC mode and global mode
4. Compatible with IE
5. Only a single exe file of 200KB size

#### Download

Download [latest release].

For <= Windows 7, download Shadowsocks-win-x.x.x.zip.

For >= Windows 8, download Shadowsocks-win-dotnet4.0-x.x.x.zip, unless you have .Net 2.0 installed.

#### Usage

1. Find Shadowsocks icon in notification tray
2. You can add multiple servers in servers menu
3. Select Enable System Proxy menu to enable system proxy
4. After you saved PAC file with any editor, Shadowsocks will notify browsers
about the change automatically
5. Please disable other proxy addons in your browser, or set them to use
system proxy

### Develop

Visual Studio Express 2012 is recommended.

#### License

GPLv3


[Appveyor]:       https://ci.appveyor.com/project/clowwindy/shadowsocks-csharp
[Build Status]:   https://ci.appveyor.com/api/projects/status/gknc8l1lxy423ehv/branch/master
[latest release]: https://sourceforge.net/projects/shadowsocksgui/files/dist/
