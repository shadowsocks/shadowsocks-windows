Shadowsocks for Windows
=======================
No-Privoxy Version

This project is forked from ShadowSocks/ShadowSocks-Windows.
The old version depends on Privoxy, although an open source project product 
(hosted on SourceForge.net), but since nobody claimed to have checked its 
source, so its  security clearness is un-guarantied, and this product, as a 
highly security-sensitive middle man, I would like it be truly reliable, so I 
removed the dependency on Privoxy.  A side-benefit of this removal is a 
shorter path to the final browsing target. In the old version, the path is like this:  
your browser ->local:1080 by shadowsocks.proxy -> local: 8033 by 
Privoxy.translator -> local:1080 by shadowsocks.forwarder -> External Socks 
server -> final target, so you can see it's a really twisted path. In the No-Privoxy 
version, the path is shortened.
Another external dependency is libsscrypto, and I happily found it to be another 
project on git, it's here: https://github.com/shadowsocks/libsscrypto . So I kept it.

This version is tested on IE 8, 11, Firefox, Chrome, and sadly it only work for 
Chrome. So if you use Chrome and care about security, then this one may be a 
better alternative.

Extra Usage Notes (besides the old  version):
1. You'd better to use [Enable System Proxy], and set mode to [Pac].
2. Added a feature to help debug, the setting is in gui-config.json,  logNetTraffic, if you 
set it to true, then the network traffic will be logged, you can then see the net traffic 
between the browser, shadowsocks, and extern socks server.
3. Added a setting in gui-config.json, pacSockHeader, its values: Socks5, Socks, Socks4,  
you should leave it as Socks5.

John Xie @Beijing


=======================
[![Build Status]][Appveyor]

[中文说明]

#### Features

1. System proxy configuration
2. PAC mode and global mode
3. [GFWList] and user rules
4. Supports HTTP proxy
5. Supports server auto switching
6. Supports UDP relay (see Usage)

#### Download

Download the [latest release].

#### Basic

1. Find Shadowsocks icon in the notification tray
2. You can add multiple servers in servers menu
3. Select `Enable System Proxy` menu to enable system proxy. Please disable other
proxy addons in your browser, or set them to use system proxy
4. You can also configure your browser proxy manually if you don't want to enable
system proxy. Set Socks5 or HTTP proxy to 127.0.0.1:1080. You can change this
port in `Servers -> Edit Servers`

#### PAC

1. You can change PAC rules by editing the PAC file. When you save the PAC file
with any editor, Shadowsocks will notify browsers about the change automatically
2. You can also update PAC file from [GFWList] (maintained by 3rd party)
3. You can also use online PAC URL

#### Server Auto Switching

1. Load balance: choosing server randomly
2. High availability: choosing the best server (low latency and packet loss)
3. Choose By Total Package Loss: ping and choose. Please also enable
   `Availability Statistics` in the menu if you want to use this
4. Write your own strategy by implement IStrategy interface and send us a pull request!

#### UDP

For UDP, you need to use SocksCap or ProxyCap to force programs you want
to be proxied to tunnel over Shadowsocks

#### Multiple Instances

If you want to manage multiple servers using other tools like SwitchyOmega,
you can start multiple Shadowsocks instances. To avoid configuration conflicts,
copy Shadowsocks to a new directory and choose a different local port.

Also, make sure to use `SOCKS5` proxy in SwitchyOmega, since we have only
one HTTP proxy instance.

#### Server Configuration

Please visit [Servers] for more information.

#### Portable Mode

If you want to put all temporary files into shadowsocks/temp folder instead of
system temp folder, create a `shadowsocks_portable_mode.txt` into shadowsocks folder.

#### Develop

Visual Studio 2015 is required.

#### License

GPLv3


[Appveyor]:       https://ci.appveyor.com/project/icylogic/shadowsocks-windows-l9mwe
[Build Status]:   https://ci.appveyor.com/api/projects/status/ytllr9yjkbpc2tu2/branch/master
[latest release]: https://github.com/shadowsocks/shadowsocks-csharp/releases
[GFWList]:        https://github.com/gfwlist/gfwlist
[Servers]:        https://github.com/shadowsocks/shadowsocks/wiki/Ports-and-Clients#linux--server-side
[中文说明]:       https://github.com/shadowsocks/shadowsocks-windows/wiki/Shadowsocks-Windows-%E4%BD%BF

%E7%94%A8%E8%AF%B4%E6%98%8E
