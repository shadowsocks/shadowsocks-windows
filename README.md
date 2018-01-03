Shadowsocks for Windows
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
7. Supports plugins

#### Download

Download the [latest release].

#### Requirements

Microsoft [.NET Framework 4.6.2] or higher. 

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
2. You can also update PAC file from [GFWList] \(maintained by 3rd party)
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

#### Plugins

If you would like to connect to server via a plugin, please set the plugin's
path (relative or absolute) on Edit Servers form.
Note: Forward Proxy will not be used while a plugin is enabled.

#### Global hotkeys

Hotkeys are NOT registered automatically. You should re-register all hotkeys after
restarting Shadowsocks. If you are using multiple instances of Shadowsocks,
you must set different key combination for each instance.

##### How to input?

1. Put focus in the corresponding textbox.
2. Press the key combination that you want to use.
3. Release all keys when you think it is ready.
4. Your input appears in the textbox.

##### How to change?

1. Put focus in the corresponding textbox.
2. Press BackSpace key to clear content.
3. Re-input new key combination.

##### How to deactivate?

1. Clear content in the textbox that you want to deactivate,
if you want to deactivate all, please clear all textboxes.
2. Press OK button to confirm.

##### Meaning of label color

- Green: This key combination is not occupied by other programs and register successfully.
- Yellow: This key combination is occupied by other programs and you have to change to another one.
- Transparent without color: The initial status.

#### Server Configuration

Please visit [Servers] for more information.

#### Develop

[Visual Studio 2015] & [.NET Framework 4.6.2 Developer Pack] are required.

#### License

GPLv3


[Appveyor]:       https://ci.appveyor.com/project/celeron533/shadowsocks-windows
[Build Status]:   https://ci.appveyor.com/api/projects/status/tfw57q6eecippsl5/branch/master?svg=true
[latest release]: https://github.com/shadowsocks/shadowsocks-csharp/releases
[GFWList]:        https://github.com/gfwlist/gfwlist
[Servers]:        https://github.com/shadowsocks/shadowsocks/wiki/Ports-and-Clients#linux--server-side
[中文说明]:       https://github.com/shadowsocks/shadowsocks-windows/wiki/Shadowsocks-Windows-%E4%BD%BF%E7%94%A8%E8%AF%B4%E6%98%8E
[.NET Framework 4.6.2]: https://www.microsoft.com/en-US/download/details.aspx?id=53344
[Visual Studio 2015]: https://www.visualstudio.com/downloads/
[.NET Framework 4.6.2 Developer Pack]: https://www.microsoft.com/download/details.aspx?id=53321
