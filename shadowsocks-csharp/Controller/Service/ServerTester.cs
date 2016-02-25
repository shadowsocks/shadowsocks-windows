using System;
using System.Text;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using System.IO;

using Shadowsocks.Model;
using Shadowsocks.Encryption;

namespace Shadowsocks.Controller.Service
{
    public class ServerTesterEventArgs : EventArgs
    {
        /// <summary>
        /// value is null when no error
        /// </summary>
        public Exception Error;

        /// <summary>
        /// consumed time on connect server
        /// </summary>
        public long ConnectionTime;

        /// <summary>
        /// total size downloaded
        /// </summary>
        public long DownloadTotal;

        /// <summary>
        /// consumed milliseconds on download
        /// </summary>
        public long DownloadMilliseconds;

        /// <summary>
        /// average speed per second
        /// </summary>
        public long DownloadSpeed;
    }

    public class ServerTesterProgressEventArgs : EventArgs
    {
        /// <summary>
        /// cancel download
        /// </summary>
        public bool Cancel;

        /// <summary>
        /// total size need download.
        /// zero when no Content-Length include in response header
        /// </summary>
        public long Total;

        /// <summary>
        /// size downloaded
        /// </summary>
        public long Download;

        /// <summary>
        /// milliseconds from download start
        /// </summary>
        public long Milliseconds;
    }

    public class ServerTesterTimeoutException : Exception
    {
        public bool Connected { get; private set; }

        public ServerTesterTimeoutException(bool connected, string msg)
            : base(msg)
        {
            Connected = connected;
        }
    }

    public class ServerTesterCancelException : Exception
    {
        public ServerTesterCancelException(string msg)
            : base(msg)
        {
        }
    }

    public class ServerTester
    {
        public int ConnectTimeout = 3000;
        public int DownloadTimeout = 4000;
        /* no ssl, url must start with "http://" */
        public string DownloadUrl = "http://dl-ssl.google.com/googletalk/googletalk-setup.exe";

        public event EventHandler<ServerTesterEventArgs> Completed;
        public event EventHandler<ServerTesterProgressEventArgs> Progress;
        public Server server;

        private long connectionTime;
        private Timer timer;
        private Socket remote;
        private IEncryptor encryptor;
        private DateTime startTime;
        private bool connected;
        private bool closed;
        private const int BufferSize = 8192;
        private byte[] RecvBuffer = new byte[BufferSize];
        private byte[] DecryptBuffer = new byte[BufferSize];
        private long contentLength;
        private long recvTotal;
        private int statusCode;
        private bool headerFinish;

        public ServerTester(Server server)
        {
            this.server = server;
        }

        public void Start()
        {
            try
            {
                closed = false;
                encryptor = EncryptorFactory.GetEncryptor(server.method, server.password, server.auth, false);
                StartConnect();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
                FireCompleted(e);
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (closed)
                    return;
                closed = true;
            }
            if (timer != null)
            {
                if (connected)
                    timer.Elapsed -= downloadTimer_Elapsed;
                else
                    timer.Elapsed -= connectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();
                timer = null;
            }
            if (remote != null)
            {
                try
                {
                    remote.Shutdown(SocketShutdown.Both);
                    remote.Close();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                }
                finally
                {
                    remote = null;
                }
            }
            if (encryptor != null)
            {
                ((IDisposable)encryptor).Dispose();
                encryptor = null;
            }
        }

        private void FireCompleted(Exception e)
        {
            if (Completed != null)
            {
                Completed(this, new ServerTesterEventArgs() { Error = e });
            }
        }

        private void FireCompleted(Exception e, long connectionTime, long downloadTotalSize, DateTime startTime)
        {
            if (Completed != null)
            {
                long milliseconds = (long)(DateTime.Now - startTime).TotalMilliseconds;
                long speed = milliseconds > 0 ? (downloadTotalSize * 1000) / milliseconds : 0;
                Completed(this, new ServerTesterEventArgs()
                {
                    Error = e,
                    ConnectionTime = connectionTime,
                    DownloadTotal = downloadTotalSize,
                    DownloadMilliseconds = milliseconds,
                    DownloadSpeed = speed
                });
            }
        }

        private void StartConnect()
        {
            lock (this)
            {
                if (closed)
                    return;
            }
            try
            {
                connected = false;

                IPAddress ipAddress;
                bool parsed = IPAddress.TryParse(server.server, out ipAddress);
                if (!parsed)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(server.server);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, server.server_port);

                remote = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

                startTime = DateTime.Now;
                timer = new Timer(ConnectTimeout);
                timer.AutoReset = false;
                timer.Elapsed += connectTimer_Elapsed;
                timer.Enabled = true;

                remote.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), timer);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
                FireCompleted(e);
            }
        }

        private void connectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (this)
            {
                if (closed)
                    return;
            }
            if (connected)
                return;
            Logging.Info($"{server.FriendlyName()} timed out");
            Close();
            FireCompleted(new ServerTesterTimeoutException(false, "Connect Server Timeout"));
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            lock (this)
            {
                if (closed)
                    return;
            }
            try
            {
                timer.Elapsed -= connectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();
                timer = null;

                remote.EndConnect(ar);

                connected = true;

                connectionTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
                StartDownload();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                FireCompleted(e);
            }
        }

        private void StartDownload()
        {
            lock (this)
            {
                if (closed)
                    return;
            }
            try
            {
                int bytesToSend;
                byte[] request = BuildRequestData(new Uri(DownloadUrl));
                byte[] buffer = new byte[request.Length + IVEncryptor.ONETIMEAUTH_BYTES + IVEncryptor.AUTH_BYTES + 32];
                encryptor.Encrypt(request, request.Length, buffer, out bytesToSend);
                timer = new Timer(DownloadTimeout);
                timer.AutoReset = false;
                timer.Elapsed += downloadTimer_Elapsed;
                timer.Enabled = true;
                startTime = DateTime.Now;
                contentLength = 0;
                recvTotal = 0;
                headerFinish = false;
                statusCode = 0;
                remote.BeginSend(buffer, 0, bytesToSend, 0, new AsyncCallback(SendCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
                FireCompleted(e);
            }
        }

        private void downloadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (this)
            {
                if (closed)
                    return;
            }
            Close();
            FireCompleted(new ServerTesterTimeoutException(true, "download timeout"),
                connectionTime, recvTotal, startTime);
        }

        private void SendCallback(IAsyncResult ar)
        {
            lock (this)
            {
                if (closed)
                    return;
            }
            try
            {
                remote.EndSend(ar);
                startTime = DateTime.Now;
                remote.BeginReceive(RecvBuffer, 0, BufferSize, 0, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
                FireCompleted(e);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            lock (this)
            {
                if (closed)
                    return;
            }
            try
            {
                int bytesRead = remote.EndReceive(ar);
                timer.Interval = DownloadTimeout;
                if (bytesRead > 0)
                {
                    int bytesLen;
                    encryptor.Decrypt(RecvBuffer, bytesRead, DecryptBuffer, out bytesLen);
                    if (!headerFinish)
                    {
                        int offset = 0;
                        headerFinish = ParseResponseHeader(DecryptBuffer,
                            ref offset, bytesLen, ref statusCode, ref contentLength);
                        if (statusCode != 200)
                        {
                            Close();
                            FireCompleted(new Exception($"server response {statusCode}"));
                            return;
                        }
                        else
                        {
                            bytesLen -= offset;
                        }
                    }
                    recvTotal += bytesLen;
                    if (Progress != null)
                    {
                        ServerTesterProgressEventArgs args = new ServerTesterProgressEventArgs()
                        {
                            Cancel = false,
                            Total = contentLength,
                            Download = recvTotal,
                            Milliseconds = (long)(DateTime.Now - startTime).TotalMilliseconds
                        };
                        Progress(this, args);
                        if (args.Cancel)
                        {
                            Close();
                            FireCompleted(new ServerTesterCancelException("Cancelled"),
                                connectionTime, recvTotal, startTime);
                            return;
                        }
                    }
                    if (contentLength > 0 && recvTotal == contentLength)
                    {
                        Close();
                        FireCompleted(null, connectionTime, recvTotal, startTime);
                        return;
                    }
                    remote.BeginReceive(RecvBuffer, 0, BufferSize, 0, new AsyncCallback(ReceiveCallback), null);
                }
                else
                {
                    Close();
                    if (contentLength == 0 || recvTotal == contentLength)
                    {
                        FireCompleted(null, connectionTime, recvTotal, startTime);
                        return;
                    }
                    else
                    {
                        FireCompleted(new Exception("Server close the connection"),
                            connectionTime, recvTotal, startTime);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
                FireCompleted(e);
            }
        }

        private string ReadLine(byte[] data, ref int offset, int len)
        {
            if (offset >= len)
                return null;
            int i = offset;
            while (i < len && data[i++] != '\n') ;
            string line = Encoding.UTF8.GetString(data, offset, i - offset).Trim();
            offset = i;
            return line;
        }

        private bool ParseResponseHeader(byte[] data, ref int offset, int len, ref int statusCode, ref long contentLength)
        {
            string line;
            if (statusCode == 0)
            {
                line = ReadLine(data, ref offset, len);
                if (line == null || !line.StartsWith("HTTP/"))
                    return false;
                string[] arr = line.Split(new char[] { ' ' });
                if (arr.Length < 3)
                    return false;
                statusCode = Convert.ToInt32(arr[1]);
            }
            while ((line = ReadLine(data, ref offset, len)) != null)
            {
                if (line == "")
                {
                    return true;
                }
                else if (line.StartsWith("Content-Length", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] arr = line.Split(new char[] { ':' });
                    contentLength = Convert.ToInt64(arr[1].Trim());
                }
            }
            return false;
        }

        private static byte[] BuildRequestData(Uri uri)
        {
            if (!string.Equals(uri.Scheme, "HTTP", StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Unsupport scheme, expect HTTP");

            string path = uri.PathAndQuery;
            string host = uri.Host;
            int port = uri.Port;
            string requestStr = $@"GET {path} HTTP/1.1
Host: {host}{(port == 80 ? string.Empty : ":" + port.ToString())}
Connection: close

";
            byte[] requestBytes = Encoding.ASCII.GetBytes(requestStr);
            byte[] domainBytes = Encoding.ASCII.GetBytes(host);
            byte[] request = new byte[4 + domainBytes.Length + requestBytes.Length];
            int i = 0;
            request[i++] = 0x03;
            request[i++] = (byte)domainBytes.Length;
            Buffer.BlockCopy(domainBytes, 0, request, i, domainBytes.Length);
            i += domainBytes.Length;
            request[i++] = (byte)((port >> 8) & 0xff);
            request[i++] = (byte)(port & 0xff);
            Buffer.BlockCopy(requestBytes, 0, request, i, requestBytes.Length);
            return request;
        }

    }
}
