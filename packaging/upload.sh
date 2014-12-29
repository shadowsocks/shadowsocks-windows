#!/bin/bash

version=$1

rsync --progress -e ssh shadowsocks-csharp/bin/x86/Release/Shadowsocks-win-dotnet4.0-$1.zip frs.sourceforge.net:/home/frs/project/shadowsocksgui/dist/
rsync --progress -e ssh shadowsocks-csharp/bin/x86/Release/Shadowsocks-win-$1.zip frs.sourceforge.net:/home/frs/project/shadowsocksgui/dist/
