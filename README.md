<img src="shadowsocks-csharp/Resources/ssw128.png" alt="[logo]" width="48"/> Shadowsocks for Windows
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

Download the latest release from [release page].

#### Requirements

Microsoft [.NET Framework 4.6.2] or higher, Microsoft [Visual C++ 2015 Redistributable] (x86) .

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
_Note_: Forward Proxy will not be used while a plugin is enabled.

#### Global hotkeys

Hotkeys could be registered automatically on startup.
If you are using multiple instances of Shadowsocks,
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

#### Development

1. [Visual Studio 2015] & [.NET Framework 4.6.2 Developer Pack] are required.
2. It is recommended to share your idea on the Issue Board before you start to work,
especially for feature development.

#### License

[GPLv3]

#### Open Source Components / Libraries

```
Caseless.Fody (MIT)    https://github.com/Fody/Caseless
Costura.Fody (MIT)     https://github.com/Fody/Costura
Fody (MIT)             https://github.com/Fody/Fody
GlobalHotKey (GPLv3)   https://github.com/kirmir/GlobalHotKey
Newtonsoft.Json (MIT)  https://www.newtonsoft.com/json
StringEx.CS ()         https://github.com/LazyMode/StringEx
ZXing.Net (Apache 2.0) https://github.com/micjahn/ZXing.Net

libsscrypto (GPLv2)    https://github.com/shadowsocks/libsscrypto
Privoxy (GPLv2)        https://www.privoxy.org
Sysproxy ()            https://github.com/Noisyfox/sysproxy
```



[Appveyor]:     https://ci.appveyor.com/project/celeron533/shadowsocks-windows
[Build Status]: https://ci.appveyor.com/api/projects/status/tfw57q6eecippsl5/branch/master?svg=true
[release page]: https://github.com/shadowsocks/shadowsocks-csharp/releases
[GFWList]:      https://github.com/gfwlist/gfwlist
[Servers]:      https://github.com/shadowsocks/shadowsocks/wiki/Ports-and-Clients#linux--server-side
[中文说明]:     https://github.com/shadowsocks/shadowsocks-windows/wiki/Shadowsocks-Windows-%E4%BD%BF%E7%94%A8%E8%AF%B4%E6%98%8E
[Visual Studio 2015]:   https://www.visualstudio.com/downloads/
[.NET Framework 4.6.2]: https://www.microsoft.com/en-US/download/details.aspx?id=53344
[.NET Framework 4.6.2 Developer Pack]: https://www.microsoft.com/download/details.aspx?id=53321
[Visual C++ 2015 Redistributable]:     https://www.microsoft.com/en-us/download/details.aspx?id=53840
[GPLv3]:        https://github.com/shadowsocks/shadowsocks-windows/blob/master/LICENSE.txt
Tactile theme
Tactile is a theme for GitHub Pages.
Download .zip
Download .tar.gz
Text can be bold, italic, or strikethrough.

Link to another page.

There should be whitespace between paragraphs.

There should be whitespace between paragraphs. We recommend including a README, or a file with information about your project.

Header 1
This is a normal paragraph following a header. GitHub is a code hosting platform for version control and collaboration. It lets you and others work together on projects from anywhere.

Header 2
This is a blockquote following a header.

When something is important enough, you do it even if the odds are not in your favor.

Header 3
// Javascript code with syntax highlighting.
var fun = function lang(l) {
  dateformat.i18n = require('./lang/' + l)
  return true;
}
# Ruby code with syntax highlighting
GitHubPages::Dependencies.gems.each do |gem, version|
  s.add_dependency(gem, "= #{version}")
end
Header 4
This is an unordered list following a header.
This is an unordered list following a header.
This is an unordered list following a header.
Header 5
This is an ordered list following a header.
This is an ordered list following a header.
This is an ordered list following a header.
Header 6
head1	head two	three
ok	good swedish fish	nice
out of stock	good and plenty	nice
ok	good oreos	hmm
ok	good zoute drop	yumm
There’s a horizontal rule below this.
Here is an unordered list:
Item foo
Item bar
Item baz
Item zip
And an ordered list:
Item one
Item two
Item three
Item four
And a nested list:
level 1 item
level 2 item
level 2 item
level 3 item
level 3 item
level 1 item
level 2 item
level 2 item
level 2 item
level 1 item
level 2 item
level 2 item
level 1 item
Small image
Octocat

Large image
Branching

Definition lists can be used with HTML syntax.
Name
Godzilla
Born
1952
Birthplace
Japan
Color
Green
Long, single-line code blocks should not wrap. They should horizontally scroll if they are too long. This line should be long enough to demonstrate this.
The final element.
Tactile theme is maintained by pages-themes
This page was generated by GitHub Pages.
