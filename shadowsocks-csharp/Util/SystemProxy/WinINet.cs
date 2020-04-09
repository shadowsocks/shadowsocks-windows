using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Shadowsocks.Util.SystemProxy
{
    public enum InternetOptions
    {
        Refresh = 37,
        SettingsChanged = 39,
        PerConnectionOption = 75,
        ProxySettingChanged = 95,
    }

    public enum InternetPerConnectionOptionEnum
    {
        Flags = 1,
        ProxyServer = 2,
        ProxyBypass = 3,
        AutoConfigUrl = 4,
        AutoDiscovery = 5,
        AutoConfigSecondaryUrl = 6,
        AutoConfigReloadDelay = 7,
        AutoConfigLastDetectTime = 8,
        AutoConfigLastDetectUrl = 9,
        FlagsUI = 10,
    }

    [Flags]
    public enum InternetPerConnectionFlags
    {
        Direct = 0x01,
        Proxy = 0x02,
        AutoProxyUrl = 0x04,
        AutoDetect = 0x08,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InternetPerConnectionOptionUnion : IDisposable
    {
        [FieldOffset(0)]
        public int dwValue;

        [FieldOffset(0)]
        public IntPtr pszValue;

        [FieldOffset(0)]
        public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (pszValue != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pszValue);
                    pszValue = IntPtr.Zero;
                }
            }
        }
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct InternetPerConnectionOption
    {
        public int dwOption;
        public InternetPerConnectionOptionUnion Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct InternetPerConnectionOptionList : IDisposable
    {
        public int Size;

        // The connection to be set. NULL means LAN.
        public IntPtr Connection;

        public int OptionCount;
        public int OptionError;

        // List of INTERNET_PER_CONN_OPTIONs.
        public System.IntPtr pOptions;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Connection != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(Connection);
                    Connection = IntPtr.Zero;
                }

                if (pOptions != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pOptions);
                    pOptions = IntPtr.Zero;
                }
            }
        }
    }

    public class WinINet
    {
        // TODO: Save, Restore,
        // TODO: Query, Set,

        public static void ProxyGlobal(string server, string bypass)
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                GetOption(InternetPerConnectionOptionEnum.FlagsUI,InternetPerConnectionFlags.Proxy),
                GetOption(InternetPerConnectionOptionEnum.ProxyServer,server),
                GetOption(InternetPerConnectionOptionEnum.ProxyBypass,bypass),
            };
            Exec(options);
        }
        public static void ProxyPAC(string url)
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                GetOption(InternetPerConnectionOptionEnum.FlagsUI,InternetPerConnectionFlags.AutoProxyUrl),
                GetOption(InternetPerConnectionOptionEnum.ProxyServer,url),
            };
            Exec(options);
        }
        public static void Direct()
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                GetOption(InternetPerConnectionOptionEnum.FlagsUI,InternetPerConnectionFlags.Direct),
            };
            Exec(options);
        }

        private static InternetPerConnectionOption GetOption(
            InternetPerConnectionOptionEnum option,
            InternetPerConnectionFlags flag
            )
        {
            return new InternetPerConnectionOption
            {
                dwOption = (int)option,
                Value =
                {
                    dwValue = (int)flag,
                }
            };
        }

        private static InternetPerConnectionOption GetOption(
            InternetPerConnectionOptionEnum option,
            string param
        )
        {
            return new InternetPerConnectionOption
            {
                dwOption = (int)option,
                Value =
                {
                    pszValue = Marshal.StringToCoTaskMemAuto(param),
                }
            };
        }

        private static void Exec(List<InternetPerConnectionOption> options)
        {
            Exec(options, null);
            foreach (var conn in RAS.GetAllConnections())
            {
                Exec(options, conn);
            }
        }

        private static void Exec(List<InternetPerConnectionOption> options, string connName)
        {
            int len = options.Sum(o => Marshal.SizeOf(o));

            IntPtr buf = Marshal.AllocCoTaskMem(len);
            IntPtr cur = buf;

            foreach (var o in options)
            {
                Marshal.StructureToPtr(o, cur, false);
                cur += Marshal.SizeOf(o);
            }
            InternetPerConnectionOptionList optionList = new InternetPerConnectionOptionList
            {
                pOptions = buf,
                Connection = string.IsNullOrEmpty(connName)
                    ? IntPtr.Zero
                    : Marshal.StringToHGlobalAuto(connName),
                OptionCount = options.Count,
                OptionError = 0,
            };
            int listSize = Marshal.SizeOf(optionList);
            optionList.Size = listSize;

            IntPtr unmanagedList = Marshal.AllocCoTaskMem(listSize);
            Marshal.StructureToPtr(optionList, unmanagedList, true);

            bool ok = InternetSetOption(
                IntPtr.Zero,
                (int)InternetOptions.PerConnectionOption,
                unmanagedList,
                listSize
            );

            Marshal.FreeCoTaskMem(buf);
            Marshal.FreeCoTaskMem(unmanagedList);

            if (!ok) throw new Exception();
            ok = InternetSetOption(
                IntPtr.Zero,
                (int)InternetOptions.ProxySettingChanged,
                IntPtr.Zero,
                0
            );
            if (!ok) throw new Exception();
            ok = InternetSetOption(
                IntPtr.Zero,
                (int)InternetOptions.Refresh,
                IntPtr.Zero,
                0
            );
            if (!ok) throw new Exception();

        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    }

}
