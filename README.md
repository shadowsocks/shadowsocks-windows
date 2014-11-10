Shadowsocks for Windows
=======================

[![Build Status]][Appveyor]

Currently beta. Please file an issue if you find any bugs.

### Features

1. Native Windows UI
2. Fast system proxy switching
3. Compatible with IE
4. Builtin PAC server with user editable PAC
5. QRCode generation (in progress)
6. Only a single exe file of 200KB size

### Download

Download [latest release].

For Windows 7 and older, please download Shadowsocks-win-x.x.x.zip.

For Windows 8.1 and newer, please download Shadowsocks-win-dotnet4.0-x.x.x.zip.

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

Copyright (C) 2014 clowwindy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.


[Appveyor]:       https://ci.appveyor.com/project/clowwindy/shadowsocks-csharp
[Build Status]:   https://ci.appveyor.com/api/projects/status/gknc8l1lxy423ehv/branch/master
[latest release]: https://sourceforge.net/projects/shadowsocksgui/files/dist/
