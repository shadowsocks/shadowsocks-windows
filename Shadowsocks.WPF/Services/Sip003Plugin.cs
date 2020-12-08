using Shadowsocks.WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Shadowsocks.WPF.Services
{
    // https://github.com/shadowsocks/shadowsocks-org/wiki/Plugin
    public sealed class Sip003Plugin : IDisposable
    {
        public IPEndPoint? LocalEndPoint { get; private set; }
        public int ProcessId => _started ? _pluginProcess.Id : 0;

        private readonly object _startProcessLock = new object();
        private readonly Process _pluginProcess;
        private bool _started;
        private bool _disposed;

        public static Sip003Plugin? CreateIfConfigured(Server server, bool showPluginOutput)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (string.IsNullOrWhiteSpace(server.Plugin))
            {
                return null;
            }

            return new Sip003Plugin(
                server.Plugin,
                server.PluginOpts,
                server.PluginArgs,
                server.Host,
                server.Port,
                showPluginOutput);
        }

        private Sip003Plugin(string plugin, string? pluginOpts, List<string>? pluginArgs, string serverAddress, int serverPort, bool showPluginOutput)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serverAddress));
            }
            if (serverPort <= 0 || serverPort > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(serverPort));
            }

            var pluginProcessStartInfo = new ProcessStartInfo
            {
                FileName = plugin,
                UseShellExecute = false,
                CreateNoWindow = !showPluginOutput,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Utils.Utilities.WorkingDirectory ?? Environment.CurrentDirectory,
                Environment =
                    {
                        ["SS_REMOTE_HOST"] = serverAddress,
                        ["SS_REMOTE_PORT"] = serverPort.ToString(),
                        ["SS_PLUGIN_OPTIONS"] = pluginOpts
                    }
            };
            if (pluginArgs != null)
                foreach (var arg in pluginArgs)
                    pluginProcessStartInfo.ArgumentList.Add(arg);

            _pluginProcess = new Process()
            {
                StartInfo = pluginProcessStartInfo,
            };
        }

        public bool StartIfNeeded()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            lock (_startProcessLock)
            {
                if (_started && !_pluginProcess.HasExited)
                {
                    return false;
                }

                var localPort = GetNextFreeTcpPort();
                LocalEndPoint = new IPEndPoint(IPAddress.Loopback, localPort);

                _pluginProcess.StartInfo.Environment["SS_LOCAL_HOST"] = LocalEndPoint.Address.ToString();
                _pluginProcess.StartInfo.Environment["SS_LOCAL_PORT"] = LocalEndPoint.Port.ToString();
                _pluginProcess.StartInfo.Arguments = ExpandEnvironmentVariables(_pluginProcess.StartInfo.Arguments, _pluginProcess.StartInfo.EnvironmentVariables);
                try
                {
                    _pluginProcess.Start();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // do not use File.Exists(...), it can not handle the scenarios when the plugin file is in system environment path.
                    // https://docs.microsoft.com/en-us/windows/win32/seccrypto/common-hresult-values
                    //if ((uint)ex.ErrorCode == 0x80004005)
                    //  https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
                    if (ex.NativeErrorCode == 0x00000002)
                    {
                        throw new FileNotFoundException("Cannot find the plugin program file", _pluginProcess.StartInfo.FileName, ex);
                    }
                    throw new ApplicationException("Plugin Program", ex);
                }
                _started = true;
            }

            return true;
        }

        public static string ExpandEnvironmentVariables(string name, StringDictionary? environmentVariables = null)
        {
            // Expand the environment variables from the new process itself
            if (environmentVariables != null)
            {
                foreach(string key in environmentVariables.Keys)
                {
                    name = name.Replace($"%{key}%", environmentVariables[key], StringComparison.OrdinalIgnoreCase);
                }
            }
            // Also expand the environment variables from current main process (system)
            name = Environment.ExpandEnvironmentVariables(name);
            return name;
        }

        public static int GetNextFreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (!_pluginProcess.HasExited)
                {
                    _pluginProcess.Kill();
                    _pluginProcess.WaitForExit();
                }
            }
            catch (Exception) { }
            finally
            {
                try
                {
                    _pluginProcess.Dispose();
                }
                catch (Exception) { }

                _disposed = true;
            }
        }
    }
}