Shadowsocks for Windows
=======================

[![Build Status]][Appveyor]

### Features

1. Native Windows UI
2. Fast system proxy switching
3. Compatible with IE
4. Builtin PAC server with user editable PAC
5. QRCode generation
6. Only a single exe file of 200KB size

### Download

Download [latest release].

For Windows 7 and older, download Shadowsocks-win-x.x.x.zip.

For Windows 8 and newer, download Shadowsocks-win-dotnet4.0-x.x.x.zip.

### Usage

1. Find Shadowsocks icon in notification tray
2. You can add multiple servers in servers menu
3. After servers are added, click Enable menu item to enable system proxy
4. After you saved PAC file with any editor, Shadowsocks will notify browsers
about the change automatically
5. Please disable other proxy addons in your browser, or set them to use
system proxy
6. You may need to install VC 2008 Runtime and .Net framework if Shadowsocks
failed to start

### License

GPLv3


[Appveyor]:       https://ci.appveyor.com/project/clowwindy/shadowsocks-csharp
[Build Status]:   https://ci.appveyor.com/api/projects/status/gknc8l1lxy423ehv/branch/master
[latest release]: https://github.com/clowwindy/shadowsocks-csharp/releases
