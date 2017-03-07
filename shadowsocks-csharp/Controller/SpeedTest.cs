using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{

    class SpeedTester
    {
        struct TransLog
        {
            public int dir;
            public int size;
        }
        public DateTime timeConnectBegin;
        public DateTime timeConnectEnd;
        public DateTime timeBeginUpload;
        public DateTime timeBeginDownload;
        public long sizeUpload = 0;
        public long sizeDownload = 0;
        public long sizeProtocolRecv = 0;
        public long sizeRecv = 0;
        private List<TransLog> sizeTransfer = new List<TransLog>();
        public string server;
        public ServerTransferTotal transfer;

        public void BeginConnect()
        {
            timeConnectBegin = DateTime.Now;
        }

        public void EndConnect()
        {
            timeConnectEnd = DateTime.Now;
        }

        public void BeginUpload()
        {
            timeBeginUpload = DateTime.Now;
        }

        public bool BeginDownload()
        {
            if (timeBeginDownload == new DateTime())
            {
                timeBeginDownload = DateTime.Now;
                return true;
            }
            return false;
        }

        public void AddDownloadSize(int size)
        {
            //if (sizeDownloadList.Count == 2)
            //    sizeDownloadList[1] = new TransLog(size, DateTime.Now);
            //else
            //    sizeDownloadList.Add(new TransLog(size, DateTime.Now));
            sizeDownload += size;
            if (transfer != null && server != null)
            {
                transfer.AddDownload(server, size);
            }
#if DEBUG
            if (sizeTransfer.Count < 128)
            {
                lock (sizeTransfer)
                {
                    sizeTransfer.Add(new TransLog { dir = 1, size = size });
                }
            }
#endif
        }
        public void AddProtocolRecvSize(int size)
        {
            sizeProtocolRecv += size;
        }

        public void AddRecvSize(int size)
        {
            sizeRecv += size;
        }

        public void AddUploadSize(int size)
        {
            sizeUpload += size;
            if (transfer != null && server != null)
            {
                transfer.AddUpload(server, size);
            }
#if DEBUG
            if (sizeTransfer.Count < 128)
            {
                lock (sizeTransfer)
                {
                    sizeTransfer.Add(new TransLog { dir = 0, size = size });
                }
            }
#endif
        }

        public string TransferLog()
        {
            string ret = "";
#if DEBUG
            int lastdir = -1;
            foreach (TransLog t in sizeTransfer)
            {
                if (t.dir != lastdir)
                {
                    lastdir = t.dir;
                    ret += (t.dir == 0 ? " u" : " d");
                }
                ret += " " + t.size.ToString();
            }
#endif
            return ret;
        }
    }

    class ProtocolResponseDetector
    {
        public enum Protocol
        {
            UNKONWN = -1,
            NOTBEGIN = 0,
            HTTP = 1,
            TLS = 2,
            SOCKS4 = 4,
            SOCKS5 = 5,
        }
        protected Protocol protocol = Protocol.NOTBEGIN;
        protected byte[] send_buffer = new byte[0];
        protected byte[] recv_buffer = new byte[0];

        public bool Pass
        {
            get; set;
        }

        public ProtocolResponseDetector()
        {
            Pass = false;
        }

        public void OnSend(byte[] send_data, int length)
        {
            if (protocol != Protocol.NOTBEGIN) return;
            Array.Resize(ref send_buffer, send_buffer.Length + length);
            Array.Copy(send_data, 0, send_buffer, send_buffer.Length - length, length);

            if (send_buffer.Length < 2) return;

            int head_size = Obfs.ObfsBase.GetHeadSize(send_buffer, send_buffer.Length);
            if (send_buffer.Length - head_size < 0) return;
            byte[] data = new byte[send_buffer.Length - head_size];
            Array.Copy(send_buffer, head_size, data, 0, data.Length);

            if (data.Length < 2) return;

            if (data.Length > 8)
            {
                if (data[0] == 22 && data[1] == 3 && (data[2] >= 0 && data[2] <= 3))
                {
                    protocol = Protocol.TLS;
                    return;
                }
                if (data[0] == 'G' && data[1] == 'E' && data[2] == 'T' && data[3] == ' '
                    || data[0] == 'P' && data[1] == 'U' && data[2] == 'T' && data[3] == ' '
                    || data[0] == 'H' && data[1] == 'E' && data[2] == 'A' && data[3] == 'D' && data[4] == ' '
                    || data[0] == 'P' && data[1] == 'O' && data[2] == 'S' && data[3] == 'T' && data[4] == ' '
                    || data[0] == 'C' && data[1] == 'O' && data[2] == 'N' && data[3] == 'N' && data[4] == 'E' && data[5] == 'C' && data[6] == 'T' && data[7] == ' '
                    )
                {
                    protocol = Protocol.HTTP;
                    return;
                }
            }
            else
            {
                protocol = Protocol.UNKONWN;
            }
        }
        public int OnRecv(byte[] recv_data, int length)
        {
            if (protocol == Protocol.UNKONWN || protocol == Protocol.NOTBEGIN) return 0;
            Array.Resize(ref recv_buffer, recv_buffer.Length + length);
            Array.Copy(recv_data, 0, recv_buffer, recv_buffer.Length - length, length);

            if (recv_buffer.Length < 2) return 0;

            if (protocol == Protocol.HTTP && recv_buffer.Length > 4)
            {
                if (recv_buffer[0] == 'H' && recv_buffer[1] == 'T' && recv_buffer[2] == 'T' && recv_buffer[3] == 'P')
                {
                    Finish();
                    return 0;
                }
                else
                {
                    protocol = Protocol.UNKONWN;
                    return 1;
                    //throw new ProtocolException("Wrong http response");
                }
            }
            else if (protocol == Protocol.TLS && recv_buffer.Length > 4)
            {
                if (recv_buffer[0] == 22 && recv_buffer[1] == 3)
                {
                    Finish();
                    return 0;
                }
                else
                {
                    protocol = Protocol.UNKONWN;
                    return 2;
                    //throw new ProtocolException("Wrong tls response");
                }
            }
            return 0;
        }

        protected void Finish()
        {
            send_buffer = null;
            recv_buffer = null;
            protocol = Protocol.UNKONWN;
            Pass = true;
        }
    }
}
