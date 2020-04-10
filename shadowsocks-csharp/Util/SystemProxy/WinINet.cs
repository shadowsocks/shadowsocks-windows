using Newtonsoft.Json;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

    public class WinINetSetting
    {
        public InternetPerConnectionFlags Flags = InternetPerConnectionFlags.Direct;
        public string ProxyServer;
        public string ProxyBypass;
        public string AutoConfigUrl;
    }

    public class WinINet
    {
        private const string SettingFile = "wininet-setting.json";
        private static WinINetSetting initialSetting;

        static WinINet()
        {
            Load();
        }

        public static void ProxyGlobal(string server, string bypass)
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                GetOption(InternetPerConnectionOptionEnum.Flags,InternetPerConnectionFlags.Proxy|InternetPerConnectionFlags.Direct),
                GetOption(InternetPerConnectionOptionEnum.ProxyServer,server),
                GetOption(InternetPerConnectionOptionEnum.ProxyBypass,bypass),
            };
            Exec(options);
        }
        public static void ProxyPAC(string url)
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                GetOption(InternetPerConnectionOptionEnum.Flags,InternetPerConnectionFlags.AutoProxyUrl|InternetPerConnectionFlags.Direct),
                GetOption(InternetPerConnectionOptionEnum.AutoConfigUrl,url),
            };
            Exec(options);
        }
        public static void Direct()
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                GetOption(InternetPerConnectionOptionEnum.Flags,InternetPerConnectionFlags.Direct),
            };
            Exec(options);
        }

        private static void Load()
        {
            try
            {
                string configContent = File.ReadAllText(Utils.GetTempPath(SettingFile));
                initialSetting = JsonConvert.DeserializeObject<WinINetSetting>(configContent);
            }
            catch (Exception)
            {
                // Suppress all exceptions. finally block will initialize new user config settings.
            }
            finally
            {
                initialSetting ??= new WinINetSetting();
            }
        }
        private static void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Utils.GetTempPath(SettingFile), FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(initialSetting, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                // logger.LogUsefulException(e);
            }
        }
        private static void Record()
        {
            initialSetting ??= Query();
        }
        public static void Restore()
        {
            Set(initialSetting);
        }

        public static void Set(WinINetSetting setting)
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                GetOption(InternetPerConnectionOptionEnum.Flags,setting.Flags),
                GetOption(InternetPerConnectionOptionEnum.ProxyServer,setting.ProxyServer),
                GetOption(InternetPerConnectionOptionEnum.ProxyBypass,setting.ProxyBypass),
                GetOption(InternetPerConnectionOptionEnum.AutoConfigUrl,setting.AutoConfigUrl),
            };
            Exec(options);
        }
        public static void Reset()
        {
            Set(new WinINetSetting
            {
                Flags = InternetPerConnectionFlags.Direct,
                ProxyServer = "",
                ProxyBypass = "",
                AutoConfigUrl = "",
            });
        }

        public static WinINetSetting Query()
        {
            List<InternetPerConnectionOption> options = new List<InternetPerConnectionOption>
            {
                new InternetPerConnectionOption{dwOption = (int)InternetPerConnectionOptionEnum.FlagsUI},
                new InternetPerConnectionOption{dwOption = (int)InternetPerConnectionOptionEnum.ProxyServer},
                new InternetPerConnectionOption{dwOption = (int)InternetPerConnectionOptionEnum.ProxyBypass},
                new InternetPerConnectionOption{dwOption = (int)InternetPerConnectionOptionEnum.AutoConfigUrl},
            };

            var (unmanagedList, listSize) = PrepareOptionList(options, null);
            bool ok = InternetQueryOption(IntPtr.Zero, (int)InternetOptions.PerConnectionOption, unmanagedList, ref listSize);
            if (!ok) throw new Exception();

            var proxy = new WinINetSetting();

            InternetPerConnectionOptionList ret = Marshal.PtrToStructure<InternetPerConnectionOptionList>(unmanagedList);
            IntPtr p = ret.pOptions;
            int nOption = ret.OptionCount;
            List<InternetPerConnectionOption> outOptions = new List<InternetPerConnectionOption>();
            for (int i = 0; i < nOption; i++)
            {
                var o = Marshal.PtrToStructure<InternetPerConnectionOption>(p);
                outOptions.Add(o);
                p += Marshal.SizeOf(o);
            }

            foreach (var o in outOptions)
            {
                switch ((InternetPerConnectionOptionEnum)o.dwOption)
                {
                    case InternetPerConnectionOptionEnum.FlagsUI:
                    case InternetPerConnectionOptionEnum.Flags:
                        proxy.Flags = (InternetPerConnectionFlags)o.Value.dwValue;
                        break;
                    case InternetPerConnectionOptionEnum.AutoConfigUrl:
                        proxy.AutoConfigUrl = Marshal.PtrToStringAuto(o.Value.pszValue);
                        break;
                    case InternetPerConnectionOptionEnum.ProxyBypass:
                        proxy.ProxyBypass = Marshal.PtrToStringAuto(o.Value.pszValue);
                        break;
                    case InternetPerConnectionOptionEnum.ProxyServer:
                        proxy.ProxyServer = Marshal.PtrToStringAuto(o.Value.pszValue);
                        break;
                    default:
                        break;
                }
            }
            return proxy;
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

        private static (IntPtr, int) PrepareOptionList(List<InternetPerConnectionOption> options, string connName)
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
                OptionCount = options.Count,
                Connection = string.IsNullOrEmpty(connName)
                    ? IntPtr.Zero
                    : Marshal.StringToHGlobalAuto(connName),
                OptionError = 0,
            };
            int listSize = Marshal.SizeOf(optionList);
            optionList.Size = listSize;

            IntPtr unmanagedList = Marshal.AllocCoTaskMem(listSize);
            Marshal.StructureToPtr(optionList, unmanagedList, true);
            return (unmanagedList, listSize);
        }

        private static void ClearOptionList(IntPtr list)
        {
            InternetPerConnectionOptionList l = Marshal.PtrToStructure<InternetPerConnectionOptionList>(list);
            Marshal.FreeCoTaskMem(l.pOptions);
            Marshal.FreeCoTaskMem(list);
        }

        private static void Exec(List<InternetPerConnectionOption> options)
        {
            // TODO: optimize load and save
            Load();
            Record();

            Exec(options, null);
            foreach (var conn in RAS.GetAllConnections())
            {
                Exec(options, conn);
            }

            Save();
        }

        private static void Exec(List<InternetPerConnectionOption> options, string connName)
        {
            var (unmanagedList, listSize) = PrepareOptionList(options, connName);

            bool ok = InternetSetOption(
                IntPtr.Zero,
                (int)InternetOptions.PerConnectionOption,
                unmanagedList,
                listSize
            );

            if (!ok)
            {
                int errno = Marshal.GetLastWin32Error();
                throw new Win32Exception(errno);
            }
            ClearOptionList(unmanagedList);
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

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetQueryOption(IntPtr hInternet, uint dwOption, IntPtr lpBuffer, ref int lpdwBufferLength);

    }
}
