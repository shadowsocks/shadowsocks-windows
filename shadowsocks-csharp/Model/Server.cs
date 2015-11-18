using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using SimpleJson;
using Shadowsocks.Controller;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;

namespace Shadowsocks.Model
{
    public class TransLog
    {
        public int size;
        public DateTime recvTime;
        public TransLog(int s, DateTime t)
        {
            size = s;
            recvTime = t;
        }
    }
    public class ServerSpeedLogShow
    {
        public long totalConnectTimes;
        public long totalDisconnectTimes;
        public long errorConnectTimes;
        public long errorTimeoutTimes;
        public long errorEncryptTimes;
        public long errorContinurousTimes;
        public long totalUploadBytes;
        public long totalDownloadBytes;
        public int sumConnectTime;
        public long avgConnectTime;
        public long avgDownloadBytes;
        public long maxDownloadBytes;
    }
    public class ServerSpeedLog
    {
        private long totalConnectTimes = 0;
        private long totalDisconnectTimes = 0;
        private long errorConnectTimes = 0;
        private long errorTimeoutTimes = 0;
        private long errorEncryptTimes = 0;
        private int  lastError = 0;
        private long errorContinurousTimes = 0;
        private long transUpload = 0;
        private long transDownload = 0;
        private List<TransLog> transLog = null;
        private long maxTransDownload = 0;
        private List<int> connectTime = null;
        private int sumConnectTime = 0;
        private List<TransLog> speedLog = null;

        public ServerSpeedLogShow Translate()
        {
            ServerSpeedLogShow ret = new ServerSpeedLogShow();
            lock (this)
            {
                ret.avgDownloadBytes = AvgDownloadBytes;
                ret.avgConnectTime = AvgConnectTime;
                ret.maxDownloadBytes = maxTransDownload;
                ret.totalConnectTimes = totalConnectTimes;
                ret.totalDisconnectTimes = totalDisconnectTimes;
                ret.errorConnectTimes = errorConnectTimes;
                ret.errorTimeoutTimes = errorTimeoutTimes;
                ret.errorEncryptTimes = errorEncryptTimes;
                ret.errorContinurousTimes = errorContinurousTimes;
                ret.totalUploadBytes = transUpload;
                ret.totalDownloadBytes = transDownload;
                ret.sumConnectTime = sumConnectTime;
            }
            return ret;
        }
        public long TotalConnectTimes
        {
            get
            {
                lock (this)
                {
                    return totalConnectTimes;
                }
            }
        }
        public long TotalDisconnectTimes
        {
            get
            {
                lock (this)
                {
                    return totalDisconnectTimes;
                }
            }
        }
        public long ErrorConnectTimes
        {
            get
            {
                lock (this)
                {
                    return errorConnectTimes;
                }
            }
        }
        public long ErrorTimeoutTimes
        {
            get
            {
                lock (this)
                {
                    return errorTimeoutTimes;
                }
            }
        }
        public long ErrorEncryptTimes
        {
            get
            {
                lock (this)
                {
                    return errorEncryptTimes;
                }
            }
        }
        public long ErrorContinurousTimes
        {
            get
            {
                lock (this)
                {
                    return errorContinurousTimes;
                }
            }
        }
        public long TotalUploadBytes
        {
            get
            {
                lock (this)
                {
                    return transUpload;
                }
            }
        }
        public long TotalDownloadBytes
        {
            get
            {
                lock (this)
                {
                    return transDownload;
                }
            }
        }
        public long AvgDownloadBytes
        {
            get
            {
                List<TransLog> transLog;
                lock (this)
                {
                    if (this.transLog == null)
                        return 0;
                    transLog = new List<TransLog>();
                    for (int i = 1; i < this.transLog.Count; ++i)
                    {
                        transLog.Add(this.transLog[i]);
                    }
                }
                {
                    long totalBytes = 0;
                    double totalTime = 0;
                    if (transLog.Count > 0 && DateTime.Now > transLog[transLog.Count - 1].recvTime.AddSeconds(10))
                    {
                        transLog.Clear();
                        return 0;
                    }
                    for (int i = 1; i < transLog.Count; ++i)
                    {
                        totalBytes += transLog[i].size;
                    }

                    {
                        long sumBytes = 0;
                        int iBeg = 0;
                        int iEnd = 0;
                        for (iEnd = 0; iEnd < transLog.Count; ++iEnd)
                        {
                            sumBytes += transLog[iEnd].size;
                            while (iBeg + 10 <= iEnd // 10 packet
                                && (transLog[iEnd].recvTime - transLog[iBeg].recvTime).TotalSeconds > 5)
                            {
                                //if ((transLog[iBeg + 1].recvTime - transLog[iBeg].recvTime).TotalMilliseconds > 20)
                                {
                                    long speed = (long)((sumBytes - transLog[iBeg].size) / (transLog[iEnd].recvTime - transLog[iBeg].recvTime).TotalSeconds);
                                    if (speed > maxTransDownload)
                                        maxTransDownload = speed;
                                }
                                sumBytes -= transLog[iBeg].size;
                                iBeg++;
                            }
                        }
                    }
                    if (transLog.Count > 1)
                        totalTime = (transLog[transLog.Count - 1].recvTime - transLog[0].recvTime).TotalSeconds;
                    if (totalTime > 1)
                    {
                        long ret = (long)(totalBytes / totalTime);
                        if (ret > maxTransDownload)
                            maxTransDownload = ret;
                        return ret;
                    }
                    else
                        return 0;
                }
            }
        }
        public long MaxDownloadBytes
        {
            get
            {
                lock (this)
                {
                    return maxTransDownload;
                }
            }
        }
        public long AvgConnectTime
        {
            get
            {
                lock (this)
                {
                    if (connectTime != null)
                    {
                        if (connectTime.Count > 4)
                        {
                            List<int> sTime = new List<int>();
                            foreach (int t in connectTime)
                            {
                                sTime.Add(t);
                            }
                            sTime.Sort();
                            int sum = 0;
                            for (int i = 0; i < connectTime.Count / 2; ++i)
                            {
                                sum += sTime[i];
                            }
                            return sum / (connectTime.Count / 2);
                        }
                        if (connectTime.Count > 0)
                            return sumConnectTime / connectTime.Count;
                    }
                    return -1;
                }
            }
        }
        public void ClearError()
        {
            lock (this)
            {
                if (totalConnectTimes > totalDisconnectTimes)
                    totalConnectTimes -= totalDisconnectTimes;
                else
                    totalConnectTimes = 0;
                totalDisconnectTimes = 0;
                errorConnectTimes = 0;
                errorTimeoutTimes = 0;
                errorEncryptTimes = 0;
                lastError = 0;
                errorContinurousTimes = 0;
            }
        }
        public void Clear()
        {
            lock (this)
            {
                if (totalConnectTimes > totalDisconnectTimes)
                    totalConnectTimes -= totalDisconnectTimes;
                else
                    totalConnectTimes = 0;
                totalDisconnectTimes = 0;
                errorConnectTimes = 0;
                errorTimeoutTimes = 0;
                errorEncryptTimes = 0;
                lastError = 0;
                errorContinurousTimes = 0;
                transUpload = 0;
                transDownload = 0;
                maxTransDownload = 0;
            }
        }
        public void AddConnectTimes()
        {
            lock (this)
            {
                totalConnectTimes += 1;
            }
        }
        public void AddDisconnectTimes()
        {
            lock (this)
            {
                totalDisconnectTimes += 1;
            }
        }
        public void AddErrorTimes()
        {
            lock (this)
            {
                errorConnectTimes += 1;
                if (lastError == 1)
                {
                    errorContinurousTimes += 1;
                }
                else
                {
                    lastError = 1;
                    errorContinurousTimes = 0;
                }
            }
        }
        public void AddTimeoutTimes()
        {
            lock (this)
            {
                errorTimeoutTimes += 1;
                if (lastError == 2)
                {
                    errorContinurousTimes += 1;
                }
                else
                {
                    lastError = 2;
                    errorContinurousTimes = 0;
                }
            }
        }
        public void AddErrorEncryptTimes()
        {
            lock (this)
            {
                errorEncryptTimes += 1;
                if (lastError == 3)
                {
                    errorContinurousTimes += 1;
                }
                else
                {
                    lastError = 3;
                    errorContinurousTimes = 0;
                }
            }
        }
        public void AddUploadBytes(long bytes)
        {
            lock (this)
            {
                transUpload += bytes;
            }
        }
        public void AddDownloadBytes(long bytes)
        {
            lock (this)
            {
                transDownload += bytes;
                if (transLog == null)
                    transLog = new List<TransLog>();
                if (transLog.Count > 0 && (DateTime.Now - transLog[transLog.Count - 1].recvTime).TotalMilliseconds < 100)
                {
                    transLog[transLog.Count - 1].size += (int)bytes;
                }
                else
                {
                    transLog.Add(new TransLog((int)bytes, DateTime.Now));
                    while (transLog.Count > 0 && DateTime.Now > transLog[0].recvTime.AddSeconds(10))
                    {
                        transLog.RemoveAt(0);
                    }
                }
            }
        }
        public void ResetContinurousTimes()
        {
            lock (this)
            {
                lastError = 0;
                errorContinurousTimes = 0;
            }
        }
        public void AddConnectTime(int millisecond)
        {
            lock (this)
            {
                if (connectTime == null)
                    connectTime = new List<int>();
                connectTime.Add(millisecond);
                sumConnectTime += millisecond;
                while (connectTime.Count > 20)
                {
                    sumConnectTime -= connectTime[0];
                    connectTime.RemoveAt(0);
                }
            }
        }
        public void AddSpeedLog(TransLog speed)
        {
            lock (this)
            {
                if (speedLog == null)
                    speedLog = new List<TransLog>();
                if (speed.size > 0)
                    speedLog.Add(speed);
                while (speedLog.Count > 20)
                {
                    speedLog.RemoveAt(0);
                }
            }
        }
    }
    public class DnsBuffer
    {
        public IPAddress ip;
        public DateTime updateTime;
        public string host;
        public bool isExpired(string host)
        {
            if (updateTime == null) return true;
            if (this.host != host) return true;
            return (DateTime.Now - updateTime).TotalMinutes > 10;
        }
        public void UpdateDns(string host, IPAddress ip)
        {
            updateTime = DateTime.Now;
            this.ip = new IPAddress(ip.GetAddressBytes());
            this.host = (string)host.Clone();
        }
    }
    public class Connections
    {
        private System.Collections.Generic.Dictionary<Socket, Int32> sockets = new Dictionary<Socket, int>();
        public bool AddRef(Socket socket)
        {
            lock (this)
            {
                if (sockets.ContainsKey(socket))
                {
                    sockets[socket] += 1;
                }
                else
                {
                    sockets[socket] = 1;
                }
                return true;
            }
        }
        public bool DecRef(Socket socket)
        {
            lock (this)
            {
                if (sockets.ContainsKey(socket))
                {
                    sockets[socket] -= 1;
                    if (sockets[socket] == 0)
                    {
                        sockets.Remove(socket);
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
        }
        public void CloseAll()
        {
            Socket[] s;
            lock (this)
            {
                s = new Socket[sockets.Count];
                sockets.Keys.CopyTo(s, 0);
            }
            foreach (Socket socket in s)
            {
                try
                {
                    //socket.Shutdown(SocketShutdown.Send);
                    socket.Shutdown(SocketShutdown.Both);
                    //socket.Close();
                }
                catch
                {

                }
            }
        }
        public int Count
        {
            get
            {
                return sockets.Count;
            }
        }
    }

    [Serializable]
    public class Server
    {
        public string server;
        public int server_port;
        public string password;
        public string method;
        public string obfs;
        public string obfsparam;
        public string remarks_base64;
        public bool tcp_over_udp;
        public bool udp_over_tcp;
        public string protocol;
        public bool obfs_udp;
        public bool enable;
        public string id;

        private object protocoldata;
        private object obfsdata;
        private ServerSpeedLog serverSpeedLog = new ServerSpeedLog();
        private DnsBuffer dnsBuffer = new DnsBuffer();
        private DnsBuffer dnsTargetBuffer = new DnsBuffer();
        private Connections Connections = new Connections();

        public void CopyServer(Server Server)
        {
            this.serverSpeedLog = Server.serverSpeedLog;
            this.dnsBuffer = Server.dnsBuffer;
            this.dnsTargetBuffer = Server.dnsTargetBuffer;
            this.Connections = Server.Connections;
            this.enable = Server.enable;
        }
        public void SetConnections(Connections Connections)
        {
            this.Connections = Connections;
        }

        public Connections GetConnections()
        {
            return Connections;
        }

        public DnsBuffer DnsBuffer()
        {
            return dnsBuffer;
        }

        public DnsBuffer DnsTargetBuffer()
        {
            return dnsTargetBuffer;
        }

        public ServerSpeedLog ServerSpeedLog()
        {
            return serverSpeedLog;
        }
        public void SetServerSpeedLog(ServerSpeedLog log)
        {
            serverSpeedLog = log;
        }
        public string remarks
        {
            get
            {
                if (this.remarks_base64.Length == 0)
                    return this.remarks_base64;
                string remarks = this.remarks_base64.Replace('-', '+').Replace('_', '/');
                try
                {
                    return Encoding.UTF8.GetString(System.Convert.FromBase64String(remarks));
                }
                catch (FormatException)
                {
                    remarks = remarks_base64;
                    remarks_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(remarks_base64)).Replace('+', '-').Replace('/', '_');
                    return remarks;
                }
            }
            set
            {
                remarks_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).Replace('+', '-').Replace('/', '_');
            }
        }
        public string FriendlyName()
        {
            if (string.IsNullOrEmpty(server))
            {
                return I18N.GetString("New server");
            }
            if (string.IsNullOrEmpty(remarks_base64))
            {
                if (server.IndexOf(':') >= 0)
                {
                    return "[" + server + "]:" + server_port;
                }
                else
                {
                    return server + ":" + server_port;
                }
            }
            else
            {
                if (server.IndexOf(':') >= 0)
                {
                    return remarks + " ([" + server + "]:" + server_port + ")";
                }
                else
                {
                    return remarks + " (" + server + ":" + server_port + ")";
                }
            }
        }

        public Server Clone()
        {
            Server ret = new Server();
            ret.server = (string)server.Clone();
            ret.server_port = server_port;
            ret.password = (string)password.Clone();
            ret.method = (string)method.Clone();
            ret.protocol = protocol;
            ret.obfs = (string)obfs.Clone();
            ret.obfsparam = (string)obfsparam.Clone();
            ret.remarks_base64 = (string)remarks_base64.Clone();
            ret.enable = enable;
            ret.udp_over_tcp = udp_over_tcp;
            ret.obfs_udp = obfs_udp;
            ret.id = id;
            ret.protocoldata = protocoldata;
            ret.obfsdata = obfsdata;
            return ret;
        }

        public Server()
        {
            this.server = "127.0.0.1";
            this.server_port = 8388;
            this.method = "aes-256-cfb";
            this.obfs = "plain";
            this.obfsparam = "";
            this.password = "0";
            this.remarks_base64 = "";
            this.udp_over_tcp = false;
            this.protocol = "origin";
            this.obfs_udp = false;
            this.enable = true;
            byte[] id = new byte[16];
            Util.Utils.RandBytes(id, id.Length);
            this.id = BitConverter.ToString(id);
        }

        public Server(string ssURL) : this()
        {
            string[] r1 = Regex.Split(ssURL, "ss://", RegexOptions.IgnoreCase);
            string base64 = r1[1].ToString();
            byte[] bytes = null;
            string data = "";
            if (base64.LastIndexOf('@') > 0)
            {
                data = base64;
            }
            else
            {
                for (var i = 0; i < 3; i++)
                {
                    try
                    {
                        bytes = System.Convert.FromBase64String(base64);
                    }
                    catch (FormatException)
                    {
                        base64 += "=";
                    }
                }
                if (bytes != null)
                {
                    data = Encoding.UTF8.GetString(bytes);
                }
            }
            if (data.Length == 0)
            {
                throw new FormatException();
            }
            try
            {
                int indexLastAt = data.LastIndexOf('@');
                int remarkIndexLastAt = data.IndexOf('#', indexLastAt);
                if (remarkIndexLastAt > 0)
                {
                    if (remarkIndexLastAt + 1 < data.Length)
                    {
                        this.remarks_base64 = data.Substring(remarkIndexLastAt + 1);
                    }
                    data = data.Substring(0, remarkIndexLastAt);
                }
                remarkIndexLastAt = data.IndexOf('/', indexLastAt);
                string param = "";
                if (remarkIndexLastAt > 0)
                {
                    if (remarkIndexLastAt + 1 < data.Length)
                    {
                        param = data.Substring(remarkIndexLastAt + 1);
                    }
                    data = data.Substring(0, remarkIndexLastAt);
                }

                string afterAt = data.Substring(indexLastAt + 1);
                int indexLastColon = afterAt.LastIndexOf(':');
                this.server_port = int.Parse(afterAt.Substring(indexLastColon + 1));
                this.server = afterAt.Substring(0, indexLastColon);

                string beforeAt = data.Substring(0, indexLastAt);
                string[] parts = beforeAt.Split(new[] { ':' });
                this.method = parts[parts.Length - 2];
                this.password = parts[parts.Length - 1];
                if (parts.Length >= 4)
                {
                    this.protocol = parts[parts.Length - 3];
                    this.obfs = parts[parts.Length - 4];
                    if (param.Length > 0)
                    {
                        this.obfsparam = Encoding.UTF8.GetString(System.Convert.FromBase64String(param.Replace('-', '+').Replace('_', '/')));
                    }
                }
                else if (parts.Length >= 3)
                {
                    string part = parts[parts.Length - 3];
                    try
                    {
                        Obfs.ObfsBase obfs = (Obfs.ObfsBase)Obfs.ObfsFactory.GetObfs(part);
                        int[] properties = obfs.GetObfs()[part];
                        if (properties[0] > 0)
                        {
                            this.protocol = part;
                        }
                        else
                        {
                            this.obfs = part;
                        }
                    }
                    catch
                    {

                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                throw new FormatException();
            }
        }

        public bool isEnable()
        {
            return enable;
        }

        public void setEnable(bool enable)
        {
            this.enable = enable;
        }

        public object getObfsData()
        {
            return this.obfsdata;
        }
        public void setObfsData(object data)
        {
            this.obfsdata = data;
        }

        public object getProtocolData()
        {
            return this.protocoldata;
        }
        public void setProtocolData(object data)
        {
            this.protocoldata = data;
        }
    }
}
