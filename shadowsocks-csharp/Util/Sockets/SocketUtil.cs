using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
namespace Shadowsocks.Util.Sockets
{
    public class HostInfo
    {
        private int ipIndex = 0;
        public string HostName { get { return hostname; } }
        private string hostname;
        private List<IPAddress> ips;
        private System.Timers.Timer timer;
        private DateTime dateTime;
        public IPAddress IP
        {
            get
            {
                return GetNextIP();          
            }
        }
        public IPAddress LastIP { get { return lastIP; } }
        private IPAddress lastIP;
        public bool IPAvailable
        {
            get
            {
                if(ips.Count>0)
                {
                    return true;
                }
                return false;
            }
        }
        public bool Refresh
        {
            set
            {
                if(value==true)
                {
                    DomainResolve();
                }
            }
        }


        private IPAddress GetNextIP()
        {
            if (ips.Count == 1)
            {
                lastIP = ips[0];
                return ips[0];
            }
            if (ips.Count > 1)
            {
                lastIP = ips[ipIndex];
                if (System.Math.Abs( DateTime.Now.Second - dateTime.Second) > 20)
                {
                    ipIndex++;
                    if (ipIndex >= ips.Count)
                    {
                        ipIndex = 0;
                    }
                }
                dateTime = DateTime.Now;
                return ips[ipIndex];
            }
            return null;
        }

        public HostInfo(string host)
        {          
            timer = new System.Timers.Timer();
            ips = new List<IPAddress>();
            hostname = host;
            timer.AutoReset = true;
            timer.Interval = 900000; //refresh ips,every 15 minutes
            timer.Elapsed += Timer_Elapsed;
            dateTime = new DateTime();
            DomainResolve();
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            DomainResolve();
            timer.Start();
        }

        private void DomainResolve()
        {
            try
            {             
                ips = new List<IPAddress>();
                Shadowsocks.Controller.Logging.Info($"Resolve domain name: {hostname}");
                IPHostEntry ip = Dns.GetHostEntry(hostname);
                Ping ping = new Ping();
                if (ip != null)
                {
                    foreach (IPAddress iPAddress in ip.AddressList)
                    {
                        PingReply reply = ping.Send(iPAddress, 6000);
                        if (reply.Status != IPStatus.TimedOut)
                        {
                            Shadowsocks.Controller.Logging.Info($"Find {iPAddress} for {hostname}");
                            ips.Add(iPAddress); 
                        }
                    }
                }
            }
            catch
            {
                Shadowsocks.Controller.Logging.Error($"Resolve domain {hostname} failed");
            }          
        }
    }
    public static class SocketUtil
    {
        private static Dictionary<string, HostInfo> dpPairs=new Dictionary<string, HostInfo>();
        public static void RefreshHostDNS(string host)
        {
            if (dpPairs.ContainsKey(host))
            {
                dpPairs[host].Refresh = true;
            }
        }
        private class DnsEndPoint2 : DnsEndPoint
        {
            public DnsEndPoint2(string host, int port) : base(host, port)
            {
            }

            public DnsEndPoint2(string host, int port, AddressFamily addressFamily) : base(host, port, addressFamily)
            {
            }

            public override string ToString()
            {
                return this.Host + ":" + this.Port;
            }
        }

       public static IPAddress GetIPAddress(string host)
        {
            try
            {
                IPAddress ipAddr;
                bool parsed = IPAddress.TryParse(host, out ipAddr);
                if(parsed)
                {
                    return ipAddr;
                }
                if(dpPairs.ContainsKey(host))
                {
                    if(dpPairs[host].IPAvailable)
                    {
                        return dpPairs[host].LastIP;
                    }
                }
                return null;
            }
            catch
            {
                Shadowsocks.Controller.Logging.Error($"Get last ip address from {host} failed");
                return null;
            }       
        }

        public static EndPoint GetServerEndPoint(string host, int port)
        {
            IPAddress ipAddress;
            bool parsed = IPAddress.TryParse(host, out ipAddress);
            if (parsed)
            {
                return new IPEndPoint(ipAddress, port);
            }
            // maybe is a domain name
            lock (dpPairs)
            {
                if (dpPairs.ContainsKey(host))
                {
                    if (dpPairs[host].IPAvailable)
                        return new IPEndPoint(dpPairs[host].IP, port);
                }
                else
                {
                    HostInfo hostInfo = new HostInfo(host);
                    dpPairs.Add(host, hostInfo);
                    if (dpPairs[host].IPAvailable)
                        return new IPEndPoint(dpPairs[host].IP, port);
                }
            }
            // maybe is a domain name
            return new DnsEndPoint2(host, port);
        }

        public static EndPoint GetEndPoint(string host, int port)
        {
            IPAddress ipAddress;
            bool parsed = IPAddress.TryParse(host, out ipAddress);
            if (parsed)
            {
                return new IPEndPoint(ipAddress, port);
            }     
            // maybe is a domain name
            return new DnsEndPoint2(host, port);
        }


        public static void FullClose(this System.Net.Sockets.Socket s)
        {
            try
            {
                s.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }
            try
            {
                s.Disconnect(false);
            }
            catch (Exception)
            {
            }
            try
            {
                s.Close();
            }
            catch (Exception)
            {
            }
        }
        
    }
}
