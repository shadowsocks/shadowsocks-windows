using System.Windows.Forms;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.IO;
using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    public static class SystemProxy
    {

        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;
        static bool _settingsReturn, _refreshReturn;

        public static void NotifyIE()
        {
            // These lines implement the Interface in the beginning of program 
            // They cause the OS to refresh the settings, causing IP to realy update
            _settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            _refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        private static readonly DateTime UnixEpoch
            = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long ToUnixEpochMilliseconds(this DateTime dt)
            => (long)(dt - UnixEpoch).TotalMilliseconds;
        private static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssfff");
        }

        public static void Update(Configuration config, bool forceDisable)
        {
            bool global = config.global;
            bool enabled = config.enabled;

            if (forceDisable)
            {
                enabled = false;
            }
            RegistryKey registry = null;
            try {
                registry = Utils.OpenUserRegKey( @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true );
                if ( registry == null ) {
                    Logging.Error( @"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" );
                    return;
                }
                if ( enabled ) {
                    if ( global ) {
                        registry.SetValue( "ProxyEnable", 1 );
                        registry.SetValue( "ProxyServer", "127.0.0.1:" + config.localPort.ToString() );
                        registry.SetValue( "AutoConfigURL", "" );
                    } else {
                        string pacUrl;
                        if ( config.useOnlinePac && ! config.pacUrl.IsNullOrEmpty() )
                            pacUrl = config.pacUrl;
                        else
                            pacUrl = $"http://127.0.0.1:{config.localPort}/pac?t={GetTimestamp( DateTime.Now )}";
                        registry.SetValue( "ProxyEnable", 0 );
                        var readProxyServer = registry.GetValue( "ProxyServer" );
                        registry.SetValue( "ProxyServer", "" );
                        registry.SetValue( "AutoConfigURL", pacUrl );
                    }
                } else {
                    registry.SetValue( "ProxyEnable", 0 );
                    registry.SetValue( "ProxyServer", "" );
                    registry.SetValue( "AutoConfigURL", "" );
                }

                //Set AutoDetectProxy
                IEAutoDetectProxy( ! enabled );

                NotifyIE();
                //Must Notify IE first, or the connections do not chanage
                CopyProxySettingFromLan();
            } catch ( Exception e ) {
                Logging.LogUsefulException( e );
                // TODO this should be moved into views
                MessageBox.Show( I18N.GetString( "Failed to update registry" ) );
            } finally {
                if ( registry != null ) {
                    try { registry.Close(); }
                    catch (Exception e)
                    { Logging.LogUsefulException(e); }
                }
            }
        }

        private static void CopyProxySettingFromLan()
        {
            RegistryKey registry = null;
            try {
                registry = Utils.OpenUserRegKey( @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections", true );
                if ( registry == null ) {
                    Logging.Error( @"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections" );
                    return;
                }
                var defaultValue = registry.GetValue( "DefaultConnectionSettings" );
                var connections = registry.GetValueNames();
                foreach ( var each in connections ) {
                    switch ( each.ToUpperInvariant() ) {
                        case "DEFAULTCONNECTIONSETTINGS":
                        case "LAN CONNECTION":
                        case "SAVEDLEGACYSETTINGS":
                            continue;
                        default:
                            //set all the connections's proxy as the lan
                            registry.SetValue( each, defaultValue );
                            continue;
                    }
                }
                NotifyIE();
            } catch ( IOException e ) {
                Logging.LogUsefulException( e );
            } finally {
                if ( registry != null ) {
                    try { registry.Close(); }
                    catch (Exception e)
                    { Logging.LogUsefulException(e); }
                }
            }
        }

        /// <summary>
        /// Checks or unchecks the IE Options Connection setting of "Automatically detect Proxy"
        /// </summary>
        /// <param name="set">Provide 'true' if you want to check the 'Automatically detect Proxy' check box. To uncheck, pass 'false'</param>
        private static void IEAutoDetectProxy(bool set)
        {
            RegistryKey registry = null;
            try {
                registry = Utils.OpenUserRegKey( @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections", true );
                if ( registry == null ) {
                    Logging.Error( @"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections" );
                    return;
                }
                var defConnection = ( byte[] ) registry.GetValue( "DefaultConnectionSettings" );
                var savedLegacySetting = ( byte[] ) registry.GetValue( "SavedLegacySettings" );

                const int versionOffset = 4;
                const int optionsOffset = 8;

                if ( set ) {
                    defConnection[ optionsOffset ] = ( byte ) ( defConnection[ optionsOffset ] | 8 );
                    savedLegacySetting[ optionsOffset ] = ( byte ) ( savedLegacySetting[ optionsOffset ] | 8 );
                } else {
                    defConnection[ optionsOffset ] = ( byte ) ( defConnection[ optionsOffset ] & ~8 );
                    savedLegacySetting[ optionsOffset ] = ( byte ) ( savedLegacySetting[ optionsOffset ] & ~8 );
                }

                BitConverter.GetBytes(unchecked( BitConverter.ToUInt32( defConnection, versionOffset ) + 1 ) )
                            .CopyTo( defConnection, versionOffset );
                BitConverter.GetBytes(unchecked( BitConverter.ToUInt32( savedLegacySetting, versionOffset ) + 1 ) )
                            .CopyTo( savedLegacySetting, versionOffset );

                registry.SetValue( "DefaultConnectionSettings", defConnection );
                registry.SetValue( "SavedLegacySettings", savedLegacySetting );
            } catch ( Exception e ) {
                Logging.LogUsefulException( e );
            } finally {
                if (registry != null)
                {
                    try { registry.Close(); }
                    catch (Exception e)
                    { Logging.LogUsefulException(e); }
                }
            }
        }
    }
}
