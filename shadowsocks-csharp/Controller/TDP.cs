using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Encryption;
using System.Timers;

namespace Shadowsocks.Controller
{
    class PacketInfo
    {
        public DateTime time;
        public byte[] data;
        public PacketInfo(byte[] data)
        {
            this.data = data;
            this.time = DateTime.Now;
        }
    }
    class SendQueue
    {
        private Dictionary<ulong, PacketInfo> id_to_Buffer = new Dictionary<ulong, PacketInfo>();
        private ulong BeginID = 0;
        private ulong EndID = 1;
        public const int MTU_MIN = 1000;
        public const int MTU_MAX = 1400;
        public double Interval = 0.5;
        private Random random = new Random();

        public ulong sendBeginID
        {
            get
            {
                lock (this)
                {
                    return BeginID;
                }
            }
        }
        public ulong sendEndID
        {
            get
            {
                lock (this)
                {
                    return EndID;
                }
            }
        }

        public void SetSendBeginID(ulong recvid)
        {
            lock (this)
            {
                for (; BeginID < recvid; ++BeginID)
                {
                    if (id_to_Buffer.ContainsKey(BeginID + 1))
                        id_to_Buffer.Remove(BeginID + 1);
                    else
                        return;
                }
            }
        }

        public bool Contains(ulong id)
        {
            lock (this)
            {
                return id_to_Buffer.ContainsKey(id);
            }
        }

        public byte[] Get(ulong id)
        {
            lock (this)
            {
                if (id_to_Buffer.ContainsKey(id))
                    return id_to_Buffer[id].data;
                return null;
            }
        }

        public List<ulong> GetDataList(List<ulong> idList)
        {
            List<ulong> ret = new List<ulong>();
            lock (this)
            {
                DateTime curTime = DateTime.Now;
                foreach (ulong id in idList)
                {
                    if (id_to_Buffer.ContainsKey(id))
                    {
                        if ((curTime - id_to_Buffer[id].time).TotalSeconds > this.Interval)
                        {
                            id_to_Buffer[id].time = DateTime.Now;
                            ret.Add(id);
                        }
                    }
                }
            }
            return ret;
        }

        public ulong PushBack(byte[] dataBuffer)
        {
            lock (this)
            {
                for (int beg_pos = 0; beg_pos < dataBuffer.Length;)
                {
                    int split_pos = beg_pos + MTU_MAX;
                    if (split_pos > dataBuffer.Length)
                        split_pos = dataBuffer.Length;
                    else
                        split_pos = beg_pos + random.Next(MTU_MIN, MTU_MAX);
                    byte[] sub_buffer = new byte[split_pos - beg_pos];
                    Array.Copy(dataBuffer, beg_pos, sub_buffer, 0, sub_buffer.Length);
                    beg_pos = split_pos;

                    id_to_Buffer[EndID] = new PacketInfo(sub_buffer);
                    ++EndID;
                }
                return EndID;
            }
        }
    }

    class RecvQueue
    {
        private Dictionary<ulong, byte[]> id_to_Buffer = new Dictionary<ulong, byte[]>();
        private SortedDictionary<ulong, bool> id_missing = new SortedDictionary<ulong, bool>();
        private ulong CallbackBeginID = 0;
        private ulong BeginID = 0;
        private ulong EndID = 1;


        public ulong recvCallbackID
        {
            get
            {
                lock (this)
                {
                    return CallbackBeginID;
                }
            }
        }
        public ulong recvBeginID
        {
            get
            {
                lock (this)
                {
                    return BeginID;
                }
            }
        }
        public ulong recvEndID
        {
            get
            {
                lock (this)
                {
                    return EndID;
                }
            }
        }

        public bool CanInsertID(ulong id)
        {
            lock (this)
            {
                return BeginID < id;
            }
        }

        public void InsertData(ulong id, byte[] data)
        {
            lock (this)
            {
                if (id_to_Buffer.ContainsKey(id))
                    return;
                if (BeginID < id)
                {
                    id_to_Buffer[id] = data;
                    if (id_missing.ContainsKey(id))
                    {
                        id_missing.Remove(id);
                    }
                    else if (EndID < id)
                    {
                        while (EndID < id)
                        {
                            id_missing[EndID] = true;
                            EndID++;
                        }
                        EndID++;
                    }
                    while (id_to_Buffer.ContainsKey(BeginID + 1))
                    {
                        BeginID++;
                    }
                }
            }
        }

        public bool HasCallbackData()
        {
            lock (this)
            {
                return CallbackBeginID < BeginID;
            }
        }

        public int GetCallbackData(byte[] data, int maxsize)
        {
            lock (this)
            {
                if (id_to_Buffer.ContainsKey(CallbackBeginID + 1))
                {
                    int len = id_to_Buffer[CallbackBeginID + 1].Length;
                    if (len > maxsize)
                        return 0;
                    CallbackBeginID++;
                    id_to_Buffer[CallbackBeginID].CopyTo(data, 0);
                    id_to_Buffer.Remove(CallbackBeginID);
                    return len;
                }
            }
            return 0;
        }

        public void SetEndID(ulong recvid)
        {
            lock (this)
            {
                while (EndID < recvid)
                {
                    id_missing[EndID] = true;
                    EndID++;
                }
            }
        }

        public List<ushort> GetMissing(ref ulong recvid)
        {
            lock (this)
            {
                List<ushort> ids = new List<ushort>();
                recvid = BeginID;
                //ulong end = EndID;
                //if (end > recvid + 1024 * 32)
                //    end = recvid + 1024 * 32;
                //for (ulong id = recvid + 1; id < end; ++id)
                //{
                //    if (this.id_to_Buffer.ContainsKey(id))
                //        continue;
                //    ids.Add((ushort)(id - recvid));
                //}
                foreach (ulong id in id_missing.Keys)
                {
                    if (id - recvid > 32768)
                        break;
                    ids.Add((ushort)(id - recvid));
                }
                return ids;
            }
        }

    }

    class TDPHandler
    {
        public enum Command
        {
            CMD_CONNECT = 0,
            CMD_RSP_CONNECT = 1,
            CMD_CONNECT_REMOTE = 2,
            CMD_RSP_CONNECT_REMOTE = 3,
            CMD_POST = 4,
            CMD_SYN_STATUS = 5,
            CMD_POST_64 = 6,
            CMD_REQ_PACKET_64 = 7,
            CMD_DISCONNECT = 8,
        }

        enum ConnectState
        {
            END = -1,
            READY = 0,
            CONNECTING = 1,
            CONNECTINGREMOTE = 2,
            WAITCONNECTINGREMOTE = 3,
            CONNECTED = 4,
            CONNECTIONEND = 5,
            DISCONNECTING = 6,
        }
        private ConnectState state = ConnectState.READY;
        //private object stateLock = new object();

        protected IEncryptor encryptor;
        protected IEncryptor decryptor;
        protected Socket sock;
        private IPEndPoint sockEndPoint;
        private string proxyServerURI;
        private int proxyServerPort;
        private string encryptMethod;
        private string encryptPassword;
        private uint requestid = 0;
        private byte[] localid = new byte[4];

        private object encryptionLock = new object();
        private object decryptionLock = new object();

        // Size of receive buffer.
        public const int RecvSize = 65536;
        public const int BufferSize = RecvSize + 32;
        // remote receive buffer
        private byte[] recvBuffer = new byte[RecvSize];
        //private object recvLock = new object();
        // remote send buffer
        //private List<byte[]> sendBufferList = new List<byte[]>();
        private LinkedList<byte[]> sendBufferList = new LinkedList<byte[]>();
        private byte[] sendConnectBuffer;
        // callback buffer
        private byte[] beginReceiveFromBuffer;

        private SendQueue id_to_sendBuffer = new SendQueue();
        private RecvQueue id_to_recvBuffer = new RecvQueue();
        //private ulong recvCallbackID = 0;

        protected Timer timer;
        protected object timerLock = new object();

        static uint logReqid = 0;

        private bool recvIdle;
        public DateTime updateTime;
        public int TTL = 60;

        private Random random = new Random();

        private AsyncCallback asyncCallBackSend;
        private object asyncCallBackSendLock = new object();
        private AsyncCallback asyncCallBackRecv;
        private object asyncCallBackRecvLock = new object();

        private int callBackBufferSize;
        private object endReceiveFromLock = new object();

        //private Object callBackState;
        private readonly double time_CONNECTING = 0.2;
        private readonly double time_CONNECTINGREMOTE = 0.2;
        private readonly double time_WAITCONNECTINGREMOTE = 0.3;
        private readonly double time_CONNECTED = 0.5;
        private readonly double time_CONNECTIONEND = 1;
        private readonly double time_DEFAULT = 1;

        private ConnectState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                if (value == ConnectState.CONNECTING)
                {
                    ResetTimeout(time_CONNECTING);
                }
                else if (value == ConnectState.WAITCONNECTINGREMOTE)
                {
                    ResetTimeout(time_WAITCONNECTINGREMOTE);
                }
                else if (value == ConnectState.CONNECTED)
                {
                    ResetTimeout(time_CONNECTED);
                }
                else if (value == ConnectState.END)
                {
                    ResetTimeout(0);
                }
            }
        }

        public AddressFamily AddressFamily
        {
            get
            {
                return sock.AddressFamily;
            }
        }
        public TDPHandler()
        {
            recvIdle = true;
        }

        private void ResetTimeout(Double time)
        {
            if (time <= 0 && timer == null)
                return;

            lock (timerLock)
            {
                if (time <= 0)
                {
                    if (timer != null)
                    {
                        timer.Enabled = false;
                        timer.Elapsed -= timer_Elapsed;
                        timer.Dispose();
                        timer = null;
                    }
                }
                else
                {
                    if (timer == null)
                    {
                        timer = new Timer(time * 1000.0);
                        timer.Elapsed += timer_Elapsed;
                        timer.Start();
                    }
                    else
                    {
                        timer.Interval = time * 1000.0;
                        timer.Stop();
                        timer.Start();
                    }
                }
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.State == ConnectState.END)
            {
                return;
            }
            Idle();
        }

        protected void Update()
        {
            updateTime = DateTime.Now;
        }

        protected bool isTimeout()
        {
            return (DateTime.Now - updateTime).TotalSeconds > TTL;
        }
        private byte[] CheckRecvData(byte[] buffer)
        {
            if (buffer[buffer.Length - 2] != buffer[2] || buffer[buffer.Length - 1] != buffer[3])
                return null;
            //ushort req_id = (ushort)(((ushort)buffer[buffer.Length - 2] << (ushort)8) + buffer[buffer.Length - 1]);
            //if (this.requestid == 0)
            //{
            //}
            //else
            //{
            //    if (req_id != this.requestid)
            //        return null;
            //}
            byte[] ret = new byte[buffer.Length - 2];
            Array.Copy(buffer, ret, buffer.Length - 2);
            return ret;
        }

        private void doRecv()
        {
            if (sock != null && recvIdle)
            {
                //lock (recvLock)
                if (sock != null && recvIdle)
                {
                    IPEndPoint sender = new IPEndPoint(sock.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                    EndPoint tempEP = (EndPoint)sender;
                    recvIdle = false;
                    sock.BeginReceiveFrom(recvBuffer, 0, RecvSize, SocketFlags.None, ref tempEP,
                        new AsyncCallback(UDPReceiveCallback), null);
                }
            }
        }

        private int endRecv(IAsyncResult ar, ref EndPoint endPoint)
        {
            if (sock != null && !recvIdle)
            {
                //lock (recvLock)
                if (sock != null && !recvIdle)
                {
                    int bytesRead = sock.EndReceiveFrom(ar, ref endPoint);
                    recvIdle = true;
                    return bytesRead;
                }
            }
            return 0;
        }
        private int tryRecv(ref EndPoint endPoint)
        {
            if (sock != null)
            {
                int bytesRead = sock.ReceiveFrom(recvBuffer, ref endPoint);
                return bytesRead;
            }
            return 0;
        }

        public static byte[] ParseUDPHeader(byte[] buffer, ref int len)
        {
            if (buffer.Length == 0)
                return buffer;
            if (buffer[0] == 0x81)
            {
                len = len - 1;
                byte[] ret = new byte[len];
                Array.Copy(buffer, 1, ret, 0, len);
                return ret;
            }
            if (buffer[0] == 0x80 && len >= 2)
            {
                int ofbs_len = buffer[1];
                if (ofbs_len + 2 < len)
                {
                    len = len - ofbs_len - 2;
                    byte[] ret = new byte[len];
                    Array.Copy(buffer, ofbs_len + 2, ret, 0, len);
                    return ret;
                }
            }
            if (buffer[0] == 0x82 && len >= 3)
            {
                int ofbs_len = (buffer[1] << 8) + buffer[2];
                if (ofbs_len + 3 < len)
                {
                    len = len - ofbs_len - 3;
                    byte[] ret = new byte[len];
                    Array.Copy(buffer, ofbs_len + 3, ret, 0, len);
                    return ret;
                }
            }
            if (len < buffer.Length)
            {
                byte[] ret = new byte[len];
                Array.Copy(buffer, ret, len);
                return ret;
            }
            return buffer;
        }

        private void RemoteSendto(byte[] bytes, int length, int insert_index, bool obfs, int obfs_max = 40)
        {
            int bytesToSend;
            byte[] bytesToEncrypt = null;
            int bytes_beg = 0;
            length -= bytes_beg;
            if (bytes[0] != 8)
            {
                bytesToEncrypt = new byte[1];
            }
            {
                bytesToEncrypt = new byte[length];
                Array.Copy(bytes, bytes_beg, bytesToEncrypt, 0, length);
            }
            Logging.LogBin(LogLevel.Debug, "remote sendto", bytesToEncrypt, length);
            byte[] sendBuffer;
            try
            {
                lock (encryptionLock)
                {
                    if (this.State == ConnectState.END)
                    {
                        return;
                    }
                    sendBuffer = new byte[BufferSize];
                    encryptor.Reset();
                    encryptor.Encrypt(bytesToEncrypt, length, sendBuffer, out bytesToSend);
                }
                sendBuffer = CreateProxyWrapper(sendBuffer, ref bytesToSend);
                byte[] buffer = sendBuffer;
                if (bytesToSend != buffer.Length)
                {
                    buffer = new byte[bytesToSend];
                    Array.Copy(sendBuffer, buffer, bytesToSend);
                }
                lock (sendBufferList)
                {
                    if (insert_index == -1)
                    {
                        sendBufferList.AddLast(buffer);
                    }
                    else
                    {
                        if (sendBufferList.Count > 0)
                        {
                            sendBufferList.AddAfter(sendBufferList.First, buffer);
                        }
                        else
                        {
                            sendBufferList.AddLast(buffer);
                        }
                    }
                    if (sendBufferList.Count == 1)
                    {
                        sock.BeginSendTo(buffer, 0, bytesToSend, 0, sockEndPoint, new AsyncCallback(UDPSendCallback), null);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Shutdown();
            }
        }

        private bool RemoveRemoteUDPRecvBufferHeader(byte[] remoteRecvBuffer, ref int bytesRead)
        {
            if (proxyServerPort > 0)
            {
                if (bytesRead < 7)
                {
                    return false;
                }
                int port = -1;
                if (remoteRecvBuffer[3] == 1)
                {
                    int head = 3 + 1 + 4 + 2;
                    bytesRead = bytesRead - head;
                    port = remoteRecvBuffer[head - 2] * 0x100 + remoteRecvBuffer[head - 1];
                    Array.Copy(remoteRecvBuffer, head, remoteRecvBuffer, 0, bytesRead);
                }
                else if (remoteRecvBuffer[3] == 4)
                {
                    int head = 3 + 1 + 16 + 2;
                    bytesRead = bytesRead - head;
                    port = remoteRecvBuffer[head - 2] * 0x100 + remoteRecvBuffer[head - 1];
                    Array.Copy(remoteRecvBuffer, head, remoteRecvBuffer, 0, bytesRead);
                }
                else if (remoteRecvBuffer[3] == 3)
                {
                    int head = 3 + 1 + 1 + remoteRecvBuffer[4] + 2;
                    bytesRead = bytesRead - head;
                    port = remoteRecvBuffer[head - 2] * 0x100 + remoteRecvBuffer[head - 1];
                    Array.Copy(remoteRecvBuffer, head, remoteRecvBuffer, 0, bytesRead);
                }
                else
                {
                    return false;
                }
                if (port != proxyServerPort)
                {
                    return false;
                }
            }
            return true;
        }


        private void UDPSendCallback(IAsyncResult ar)
        {
            if (this.State == ConnectState.END)
            {
                return;
            }
            try
            {
                sock.EndSendTo(ar);
                lock (sendBufferList)
                {
                    sendBufferList.RemoveFirst();
                    if (sendBufferList.Count > 0)
                    {
                        sock.BeginSendTo(sendBufferList.First.Value, 0, sendBufferList.First.Value.Length, 0, sockEndPoint, new AsyncCallback(UDPSendCallback), null);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Shutdown();
            }
        }

        private void UDPReceiveCallback(IAsyncResult ar)
        {
            if (this.State == ConnectState.END)
            {
                return;
            }
            try
            {
                IPEndPoint sender = new IPEndPoint(sock.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                EndPoint tempEP = (EndPoint)sender;

                int bytesRead = endRecv(ar, ref tempEP);

                if (bytesRead > 0)
                {
                    Update();
                    while (bytesRead > 0)
                    {
                        if (RemoveRemoteUDPRecvBufferHeader(recvBuffer, ref bytesRead))
                        {
                            int bytesRecv;
                            byte[] decryptBuffer = new byte[RecvSize];
                            lock (decryptionLock)
                            {
                                if (this.State == ConnectState.END)
                                {
                                    return;
                                }
                                decryptor.Reset();
                                decryptor.Decrypt(recvBuffer, bytesRead, decryptBuffer, out bytesRecv);
                                decryptBuffer = ParseUDPHeader(decryptBuffer, ref bytesRecv);
                            }

                            if (bytesRecv > 6)
                            {
                                //decryptBuffer = CheckCRC32(decryptBuffer);
                                //if (decryptBuffer != null)
                                byte[] debugBuffer = decryptBuffer;
                                decryptBuffer = CheckRecvData(decryptBuffer);
                                if (decryptBuffer != null)
                                {
                                    HandleReceive(decryptBuffer);
                                    //doRecv();
                                    return;
                                }
                                //else
                                //{
                                //    break;
                                //}
                            }
                            if (DataCallbackRecvInvoke())
                                return;
                        }
                        //else
                        //{
                        //    break;
                        //}
                        //bytesRead = tryRecv(ref tempEP);
                        break;
                    }
                    doRecv();
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Shutdown();
            }
        }

        private void HandleReceive(byte[] buffer)
        {
            if (buffer[0] == 8)
            {
                if ((Command)buffer[1] == Command.CMD_DISCONNECT)
                {
                    if (requestid == 0)
                    {
                    }
                    else
                    {
                        uint reqid = ((uint)buffer[2] << 8) + buffer[3];
                        if (reqid == requestid)
                        {
                            System.Diagnostics.Debug.Write("HandleReceive DISCONNECT\r\n");
                            this.State = ConnectState.DISCONNECTING;
                        }
                    }
                }
                if (this.State == ConnectState.CONNECTING)
                {
                    if ((Command)buffer[1] == Command.CMD_RSP_CONNECT
                        && requestid == 0)
                    {
                        if (buffer[4] == 1)
                        {
                            requestid = ((uint)buffer[2] << 8) + buffer[3];
                            this.State = ConnectState.CONNECTINGREMOTE;
                            if (!CallbackSendInvoke())
                            {
                                throw new SocketException((int)SocketError.ConnectionAborted);
                            }
                            if (logReqid == 0)
                                logReqid = requestid;
                        }
                        else //if (buffer[4] == 0 || buffer[4] == 3 || buffer[4] == 4 || buffer[4] == 5)
                        {
                            throw new SocketException((int)SocketError.ConnectionAborted);
                        }
                    }
                    return;
                }
                else if (this.State == ConnectState.WAITCONNECTINGREMOTE)
                {
                    if ((Command)buffer[1] == Command.CMD_RSP_CONNECT_REMOTE
                        && requestid != 0)
                    {
                        if (buffer[4] == 2)
                        {
                            uint reqid = ((uint)buffer[2] << 8) + buffer[3];
                            if (reqid == requestid)
                            {
                                this.State = ConnectState.CONNECTED;
                                {
                                    for (uint id = 1; id < id_to_sendBuffer.sendEndID; ++id)
                                    {
                                        SendData(id);
                                        if (id >= 1024) break;
                                    }
                                }
                            }
                        }
                        else if (buffer[4] == 0 || buffer[4] == 3 || buffer[4] == 4)
                        {
                            throw new SocketException((int)SocketError.ConnectionAborted);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else if (this.State == ConnectState.CONNECTED)
                {
                    uint reqid = ((uint)buffer[2] << 8) + buffer[3];
                    if (reqid == requestid)
                    {
                        if ((Command)buffer[1] == Command.CMD_POST)
                        {
                            int beg_index = 4 + 8;
                            uint recv_id = ((uint)buffer[4] << 24) + ((uint)buffer[5] << 16) + ((uint)buffer[6] << 8) + buffer[7];
                            uint pack_id = ((uint)buffer[8] << 24) + ((uint)buffer[9] << 16) + ((uint)buffer[10] << 8) + buffer[11];
                            id_to_sendBuffer.SetSendBeginID(recv_id);
                            id_to_recvBuffer.SetEndID(pack_id + 1);
                            if (id_to_recvBuffer.CanInsertID(pack_id))
                            {
                                byte[] ret_buf = new byte[buffer.Length - beg_index];
                                Array.Copy(buffer, beg_index, ret_buf, 0, ret_buf.Length);
                                id_to_recvBuffer.InsertData(pack_id, ret_buf);
                            }
                            //if (true
                            //    //requestid == logReqid && logReqid != 0
                            //    //&& (this.sendBeginID + 1 < this.sendEndID || !id_to_recvBuffer.Empty())
                            //    )
                            //    System.Diagnostics.Debug.Write("HandleReceive CMD_POST"
                            //        + " req=" + requestid
                            //        + " p_recv=" + recv_id
                            //        + " id=" + pack_id
                            //        + " pack " + pack_id.ToString() + " "
                            //        + " send " + id_to_sendBuffer.sendBeginID.ToString() + " "
                            //        + id_to_sendBuffer.sendEndID.ToString()
                            //        + " recv " + id_to_recvBuffer.recvCallbackID + " " + id_to_recvBuffer.recvBeginID.ToString() + " "
                            //        + id_to_recvBuffer.recvEndID.ToString()
                            //        + "\r\n"
                            //        );
                        }
                        else if ((Command)buffer[1] == Command.CMD_SYN_STATUS)
                        {
                            int beg_index = 4 + 8;
                            uint recv_id = ((uint)buffer[4] << 24) + ((uint)buffer[5] << 16) + ((uint)buffer[6] << 8) + buffer[7];
                            uint send_id = ((uint)buffer[8] << 24) + ((uint)buffer[9] << 16) + ((uint)buffer[10] << 8) + buffer[11];
                            id_to_sendBuffer.SetSendBeginID(recv_id);
                            id_to_recvBuffer.SetEndID(send_id);

                            int id_count = (buffer.Length - beg_index) / 2;

                            //if (true
                            //    //requestid == logReqid && logReqid != 0
                            //    //&& (this.sendBeginID + 1 < this.sendEndID || !id_to_recvBuffer.Empty())
                            //    )
                            //    System.Diagnostics.Debug.Write("HandleReceive CMD_SYN_STATUS"
                            //        + " req=" + requestid
                            //        + " p_recv=" + recv_id
                            //        + " p_send=" + send_id
                            //        + " size=" + id_count.ToString()
                            //        + " send " + id_to_sendBuffer.sendBeginID.ToString() + " "
                            //        + id_to_sendBuffer.sendEndID.ToString()
                            //        + " recv " + id_to_recvBuffer.recvCallbackID + " " + id_to_recvBuffer.recvBeginID.ToString() + " "
                            //        + id_to_recvBuffer.recvEndID.ToString()
                            //        + "\r\n"
                            //        );
                            List<ulong> idList = new List<ulong>();
                            for (int index = 0; index < id_count; ++index)
                            {
                                ulong id = recv_id + buffer[beg_index + index * 2] * 0x100u + buffer[beg_index + index * 2 + 1];
                                idList.Add(id);
                            }
                            List<ulong> packetList = this.id_to_sendBuffer.GetDataList(idList);
                            foreach (ulong id in packetList)
                            {
                                SendData(id);
                            }

                            //for (int index = 0; index < id_count; ++index)
                            //{
                            //    ulong id = recv_id + buffer[beg_index + index * 2] * 0x100u + buffer[beg_index + index * 2 + 1];
                            //    if (id > id_to_sendBuffer.sendBeginID)
                            //    {
                            //        if (this.id_to_sendBuffer.Contains(id))
                            //        {
                            //            if (requestid == logReqid && logReqid != 0)
                            //                System.Diagnostics.Debug.Write("HandleReceive "
                            //                    + " req=" + requestid
                            //                    + " send " + id.ToString()
                            //                    + "\r\n"
                            //                    );
                            //            SendData(id);
                            //        }
                            //        else
                            //        {
                            //            if (requestid == logReqid && logReqid != 0)
                            //                System.Diagnostics.Debug.Write("HandleReceive MISSING"
                            //                    + " req=" + requestid
                            //                    + " send " + id.ToString()
                            //                    + "\r\n"
                            //                    );
                            //        }
                            //    }
                            //}
                        }
                    }
                }
            }
            if (DataCallbackRecvInvoke())
            {
                return;
            }
            else if (this.State == ConnectState.DISCONNECTING)
            {
                CallbackRecvInvoke();
            }
            doRecv();
        }

        private byte[] CreateProxyWrapper(byte[] data, ref int bytesToSend)
        {
            if (proxyServerPort == 0)
                return data;

            byte[] bytesToEncrypt;
            int bytes_beg = 3;

            IPAddress ipAddress;
            string serverURI = proxyServerURI;
            int serverPort = proxyServerPort;
            bool parsed = IPAddress.TryParse(serverURI, out ipAddress);
            if (!parsed)
            {
                bytesToEncrypt = new byte[bytes_beg + 1 + 1 + serverURI.Length + 2 + bytesToSend];
                Array.Copy(data, 0, bytesToEncrypt, bytes_beg + 1 + 1 + serverURI.Length + 2, bytesToSend);
                bytesToEncrypt[0] = 0;
                bytesToEncrypt[1] = 0;
                bytesToEncrypt[2] = 0;
                bytesToEncrypt[3] = (byte)3;
                bytesToEncrypt[4] = (byte)serverURI.Length;
                for (int i = 0; i < serverURI.Length; ++i)
                {
                    bytesToEncrypt[5 + i] = (byte)serverURI[i];
                }
                bytesToEncrypt[5 + serverURI.Length] = (byte)(serverPort / 0x100);
                bytesToEncrypt[5 + serverURI.Length + 1] = (byte)(serverPort % 0x100);
            }
            else
            {
                byte[] addBytes = ipAddress.GetAddressBytes();
                bytesToEncrypt = new byte[bytes_beg + 1 + addBytes.Length + 2 + bytesToSend];
                Array.Copy(data, 0, bytesToEncrypt, bytes_beg + 1 + addBytes.Length + 2, bytesToSend);
                bytesToEncrypt[0] = 0;
                bytesToEncrypt[1] = 0;
                bytesToEncrypt[2] = 0;
                bytesToEncrypt[3] = ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? (byte)4 : (byte)1;
                for (int i = 0; i < addBytes.Length; ++i)
                {
                    bytesToEncrypt[4 + i] = addBytes[i];
                }
                bytesToEncrypt[4 + addBytes.Length] = (byte)(serverPort / 0x100);
                bytesToEncrypt[4 + addBytes.Length + 1] = (byte)(serverPort % 0x100);
            }

            bytesToSend = bytesToEncrypt.Length;
            //Array.Copy(bytesToEncrypt, connetionSendBuffer, bytesToSend);
            return bytesToEncrypt;

            //if (proxyEndPoint.AddressFamily == AddressFamily.InterNetwork)
            //{
            //    byte[] buffer = new byte[4 + 4 + 2 + data.Length];
            //    data.CopyTo(buffer, 4 + 4 + 2);
            //    buffer[3] = 1;
            //    proxyEndPoint.Address.GetAddressBytes().CopyTo(buffer, 4);
            //    buffer[4 + 4] = (byte)(proxyEndPoint.Port >> 8);
            //    buffer[4 + 4 + 1] = (byte)(proxyEndPoint.Port);
            //    return data;
            //}
            //else
            //{
            //    byte[] buffer = new byte[4 + 16 + 2 + data.Length];
            //    data.CopyTo(buffer, 4 + 16 + 2);
            //    buffer[3] = 4;
            //    proxyEndPoint.Address.GetAddressBytes().CopyTo(buffer, 4);
            //    buffer[4 + 16] = (byte)(proxyEndPoint.Port >> 8);
            //    buffer[4 + 16 + 1] = (byte)(proxyEndPoint.Port);
            //    return data;
            //}
        }

        private byte[] CreateConnectData()
        {
            int size = random.Next(64);
            byte[] buffer = new byte[4 + 4 + size + 4];
            buffer[0] = 0x8;
            buffer[1] = (byte)Command.CMD_CONNECT;
            localid.CopyTo(buffer, 4);
            Util.CRC32.SetCRC32(buffer);
            return buffer;
        }

        private byte[] CreateRequestConnectData(byte[] connectInfo)
        {
            int size = random.Next(64);
            byte[] buffer = new byte[4 + 4 + connectInfo.Length + size + 4];
            buffer[0] = 0x8;
            buffer[1] = (byte)Command.CMD_CONNECT_REMOTE;
            buffer[2] = (byte)(requestid / 256);
            buffer[3] = (byte)(requestid % 256);
            localid.CopyTo(buffer, 4);
            Array.Copy(connectInfo, 0, buffer, 8, connectInfo.Length);
            Util.CRC32.SetCRC32(buffer);
            return buffer;
        }

        private byte[] CreateCloseConnectData()
        {
            int size = random.Next(64);
            byte[] buffer = new byte[4 + 4 + size + 4];
            buffer[0] = 0x8;
            buffer[1] = (byte)Command.CMD_DISCONNECT;
            buffer[2] = (byte)(requestid / 256);
            buffer[3] = (byte)(requestid % 256);
            localid.CopyTo(buffer, 4);
            Util.CRC32.SetCRC32(buffer);
            return buffer;
        }

        private byte[] CreateSendData(ulong id, byte[] data)
        {
            if (data == null)
                return null;
            byte[] buffer;
            ulong recvid = id_to_recvBuffer.recvBeginID;
            int beginIndex;
            if (id > 0xffffffff || recvid > 0xffffffff)
            {
                beginIndex = 8 + 16;
                buffer = new byte[beginIndex + data.Length + 4];
                buffer[0] = 0x8;
                buffer[1] = (byte)Command.CMD_POST_64;
                buffer[2] = (byte)(requestid / 256);
                buffer[3] = (byte)(requestid % 256);
                byte[] bytes = BitConverter.GetBytes(recvid);
                Array.Reverse(bytes);
                Array.Copy(bytes, 0, buffer, 8, 8);
                bytes = BitConverter.GetBytes(id);
                Array.Reverse(bytes);
                Array.Copy(bytes, 0, buffer, 16, 8);
            }
            else
            {
                beginIndex = 8 + 8;
                buffer = new byte[beginIndex + data.Length + 4];
                buffer[0] = 0x8;
                buffer[1] = (byte)Command.CMD_POST;
                buffer[2] = (byte)(requestid / 256);
                buffer[3] = (byte)(requestid % 256);

                buffer[8] = (byte)((recvid >> 24) & 0xff);
                buffer[9] = (byte)((recvid >> 16) & 0xff);
                buffer[10] = (byte)((recvid >> 8) & 0xff);
                buffer[11] = (byte)((recvid) & 0xff);

                buffer[12] = (byte)((id >> 24) & 0xff);
                buffer[13] = (byte)((id >> 16) & 0xff);
                buffer[14] = (byte)((id >> 8) & 0xff);
                buffer[15] = (byte)((id) & 0xff);
            }
            localid.CopyTo(buffer, 4);
            Array.Copy(data, 0, buffer, beginIndex, data.Length);
            Util.CRC32.SetCRC32(buffer);
            return buffer;
        }

        private byte[] CreateRndData(byte[] data)
        {
            byte[] buffer;
            int length = random.Next(1024);
            if (length == 0) return data;
            else if (length == 1)
            {
                buffer = new byte[data.Length + 1];
                data.CopyTo(buffer, 1);
                buffer[0] = 0x81;
            }
            else if (length < 256)
            {
                buffer = new byte[data.Length + length];
                data.CopyTo(buffer, length);
                buffer[0] = 0x80;
                buffer[1] = (byte)length;
            }
            else
            {
                buffer = new byte[data.Length + length];
                data.CopyTo(buffer, length);
                buffer[0] = 0x82;
                buffer[1] = (byte)(length >> 8);
                buffer[2] = (byte)(length);
            }
            return buffer;
        }

        private byte[] CreateSyncData()
        {
            byte[] buffer;
            ulong recvid = 0;
            ulong sendid = id_to_sendBuffer.sendEndID;
            if (sendid > 0xffffffff || id_to_recvBuffer.recvEndID > 0xffffffff)
            {
                // 64
            }
            {
                List<ushort> ids = id_to_recvBuffer.GetMissing(ref recvid);

                buffer = new byte[8 + 8 + ids.Count * 2 + 4];
                buffer[0] = 0x8;
                buffer[1] = (byte)Command.CMD_SYN_STATUS;
                buffer[2] = (byte)(requestid / 256);
                buffer[3] = (byte)(requestid % 256);

                buffer[8] = (byte)((recvid >> 24) & 0xff);
                buffer[9] = (byte)((recvid >> 16) & 0xff);
                buffer[10] = (byte)((recvid >> 8) & 0xff);
                buffer[11] = (byte)((recvid) & 0xff);

                buffer[12] = (byte)((sendid >> 24) & 0xff);
                buffer[13] = (byte)((sendid >> 16) & 0xff);
                buffer[14] = (byte)((sendid >> 8) & 0xff);
                buffer[15] = (byte)((sendid) & 0xff);

                for (int index = 0; index < ids.Count; ++index)
                {
                    buffer[16 + index * 2] = (byte)(ids[index] >> 8);
                    buffer[16 + index * 2 + 1] = (byte)(ids[index] & 0xff);
                }
                //if (requestid == logReqid && logReqid != 0
                //    //&& (this.sendBeginID + 1 < this.sendEndID || !id_to_recvBuffer.Empty())
                //    )
                //    System.Diagnostics.Debug.Write("CreateSyncData"
                //        + " req=" + requestid
                //        + " size=" + ids.Count.ToString()
                //        + " send " + id_to_sendBuffer.sendBeginID.ToString() + " "
                //        + id_to_sendBuffer.sendEndID.ToString()
                //        + " recv " + id_to_recvBuffer.recvBeginID.ToString() + " "
                //        + id_to_recvBuffer.recvEndID.ToString()
                //        + "\r\n"
                //        );
            }
            localid.CopyTo(buffer, 4);
            Util.CRC32.SetCRC32(buffer);
            return buffer;
        }

        private void SendData(ulong id)
        {
            byte[] buf = CreateSendData(id, id_to_sendBuffer.Get(id));
            if (buf != null) RemoteSendto(buf, buf.Length, -1, false);
            if (id <= 16)
            {
                buf = CreateSendData(id, id_to_sendBuffer.Get(id));
                if (buf != null) RemoteSendto(buf, buf.Length, -1, false);
            }
        }

        public void BeginConnect(string method, string password, IPEndPoint ep, string proxyServerURI, int proxyServerPort, AsyncCallback callback, Object state)
        {
            if (this.State == ConnectState.END)
            {
                throw new SocketException((int)SocketError.ConnectionAborted);
            }
            Update();
            {
                if (this.State == ConnectState.READY)
                {
                    encryptMethod = method;
                    encryptPassword = password;
                    this.encryptor = EncryptorFactory.GetEncryptor(method, password);
                    this.decryptor = EncryptorFactory.GetEncryptor(method, password);
                    this.sockEndPoint = ep;
                    this.proxyServerURI = proxyServerURI;
                    this.proxyServerPort = proxyServerPort;

                    sock = new Socket(ep.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    sock.SendBufferSize = 1024 * 1024 * 4;
                    sock.ReceiveBufferSize = 1024 * 1024 * 4;

                    random.NextBytes(localid);
                    byte[] sendBuffer = CreateConnectData();

                    lock (asyncCallBackSendLock)
                    {
                        this.asyncCallBackSend = callback;
                        //this.callBackState = state;
                    }

                    RemoteSendto(sendBuffer, sendBuffer.Length, -1, false);
                    doRecv();
                    this.State = ConnectState.CONNECTING;
                }
            }
        }
        public void EndConnect(IAsyncResult ar)
        {
            if (this.State == ConnectState.END)
            {
                throw new SocketException((int)SocketError.ConnectionAborted);
            }
        }

        public void Shutdown()
        {
            if (this.State != ConnectState.END && this.State != ConnectState.DISCONNECTING)
            {
                this.State = ConnectState.CONNECTIONEND;
            }

            CallbackSendInvoke();
            if (DataCallbackRecvInvoke())
                return;
        }

        public void Close()
        {
            lock (this)
            {
                if (this.State == ConnectState.END)
                {
                    return;
                }
                else
                {
                    this.State = ConnectState.END;
                }
            }

            sendBufferList = new LinkedList<byte[]>();
            lock (encryptionLock)
            {
                lock (decryptionLock)
                {
                    if (encryptor != null)
                        ((IDisposable)encryptor).Dispose();
                    if (decryptor != null)
                        ((IDisposable)decryptor).Dispose();
                }
            }

            CallbackSendInvoke();
            CallbackRecvInvoke();

            lock (this)
            {
                if (sock != null)
                {
                    try
                    {
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                    sock = null;
                }
            }

            id_to_sendBuffer = new SendQueue();
            id_to_recvBuffer = new RecvQueue();
        }

        private bool CallbackSendInvoke()
        {
            AsyncCallback lastCallback = null;
            lock (asyncCallBackSendLock)
            {
                if (this.asyncCallBackSend != null)
                {
                    Update();
                    lastCallback = this.asyncCallBackSend;
                    this.asyncCallBackSend = null;
                }
            }
            if (lastCallback != null)
            {
                lastCallback.Invoke(null); //ConnectCallback
                return true;
            }
            return false;
        }

        private bool CallbackRecvInvoke()
        {
            AsyncCallback lastCallback = null;
            lock (asyncCallBackRecvLock)
            {
                if (this.asyncCallBackRecv != null)
                {
                    Update();
                    lastCallback = this.asyncCallBackRecv;
                    this.asyncCallBackRecv = null;
                }
            }
            if (lastCallback != null)
            {
                lastCallback.Invoke(null);
                return true;
            }
            return false;
        }

        private bool DataCallbackRecvInvoke()
        {
            lock (asyncCallBackRecvLock)
            {
                if (id_to_recvBuffer.HasCallbackData())
                {
                    return CallbackRecvInvoke();
                }
            }
            return false;
        }

        public void BeginSendTo(byte[] sendbuffer, int size, AsyncCallback callback, object state)
        {
            if (this.State == ConnectState.END)
            {
                throw new SocketException((int)SocketError.ConnectionAborted);
            }
            Update();
            lock (this)
            {
                lock (asyncCallBackSendLock)
                {
                    this.asyncCallBackSend = callback;
                    //this.callBackState = state;
                }
                byte[] buffer = new byte[size];
                Array.Copy(sendbuffer, 0, buffer, 0, size);
                if (this.State == ConnectState.CONNECTINGREMOTE)
                {
                    if (sendConnectBuffer == null)
                    {
                        int headerSize = 0;
                        if (buffer[0] == 3)
                        {
                            headerSize = 2 + buffer[1] + 2;
                        }
                        else if (buffer[0] == 1)
                        {
                            headerSize = 1 + 4 + 2;
                        }
                        else if (buffer[0] == 4)
                        {
                            headerSize = 1 + 16 + 2;
                        }
                        if (headerSize < buffer.Length)
                        {
                            byte[] dataBuffer = new byte[buffer.Length - headerSize];
                            Array.Copy(buffer, headerSize, dataBuffer, 0, dataBuffer.Length);
                            buffer = new byte[headerSize];
                            Array.Copy(sendbuffer, buffer, headerSize);
                            id_to_sendBuffer.PushBack(dataBuffer);
                        }
                        sendConnectBuffer = buffer;
                    }
                    else
                    {
                        id_to_sendBuffer.PushBack(buffer);
                    }
                    this.State = ConnectState.WAITCONNECTINGREMOTE;
                    for (int i = 0; i < 2; ++i)
                    {
                        byte[] sendBuffer = CreateRequestConnectData(buffer);
                        RemoteSendto(sendBuffer, sendBuffer.Length, -1, false);
                    }
                }
                else if (this.State == ConnectState.WAITCONNECTINGREMOTE || this.State == ConnectState.CONNECTED)
                {
                    if (this.State == ConnectState.WAITCONNECTINGREMOTE)
                    {
                        id_to_sendBuffer.PushBack(buffer);
                    }
                    else if (this.State == ConnectState.CONNECTED)
                    {
                        ulong end_id = id_to_sendBuffer.PushBack(buffer);
                        ulong beg_id = id_to_sendBuffer.sendBeginID;
                        if (beg_id + 1024 >= end_id)
                            SendData(end_id - 1);
                    }
                }
                CallbackSendInvoke();
            }
        }

        public void BeginReceiveFrom(byte[] buffer, int size, AsyncCallback callback, object state)
        {
            lock (asyncCallBackRecvLock)
            {
                beginReceiveFromBuffer = buffer;
                this.asyncCallBackRecv = callback;
                this.callBackBufferSize = size;
            }
            if (DataCallbackRecvInvoke())
                return;
            if (this.State == ConnectState.END)
            {
                throw new SocketException((int)SocketError.ConnectionAborted);
            }
            if (this.State == ConnectState.CONNECTIONEND)
            {
                CallbackRecvInvoke();
            }
            else
            {
                doRecv();
            }
        }

        public int EndReceiveFrom(IAsyncResult ar, ref EndPoint endPoint)
        {
            return id_to_recvBuffer.GetCallbackData(beginReceiveFromBuffer, callBackBufferSize);

            //int total_len = 0;
            //lock (endReceiveFromLock)
            //{
            //    byte[] recvBuffer = new byte[callBackBufferSize];
            //    while (total_len < callBackBufferSize)
            //    {
            //        int len = id_to_recvBuffer.GetCallbackData(recvBuffer, callBackBufferSize - total_len); //this.id_to_recvBuffer[recvCallbackID].Length;
            //        if (len == 0)
            //            break;
            //        Array.Copy(recvBuffer, 0, beginReceiveFromBuffer, total_len, len);
            //        total_len += len;

            //        if (requestid == logReqid && logReqid != 0)
            //        {
            //            System.Diagnostics.Debug.Write("EndReceiveFrom"
            //                + " req=" + requestid
            //                + " send " + id_to_sendBuffer.sendBeginID.ToString() + " "
            //                + id_to_sendBuffer.sendEndID.ToString()
            //                + " recv " + id_to_recvBuffer.recvCallbackID.ToString() + " "
            //                + id_to_recvBuffer.recvEndID.ToString()
            //                + "  " + id_to_recvBuffer.ToString()
            //                + "\r\n"
            //                );
            //        }
            //    }
            //}
            //return total_len;
        }

        protected void Idle()
        {
            //if (requestid == logReqid && logReqid != 0)
            //{
            //    System.Diagnostics.Debug.Write("Idle" + requestid + " " + this.State + "\r\n");
            //}
            if (//this.State == ConnectState.DISCONNECTING ||
                this.State == ConnectState.END)
            {
                ResetTimeout(0);
                return;
            }
            if (isTimeout())
            {
                Close();
            }
            if (this.State == ConnectState.READY)
            {
                ResetTimeout(0);
            }
            else if (this.State == ConnectState.CONNECTING)
            {
                byte[] sendBuffer = CreateConnectData();
                RemoteSendto(sendBuffer, sendBuffer.Length, -1, false);
                ResetTimeout(time_CONNECTING);
            }
            else if (this.State == ConnectState.CONNECTINGREMOTE)
            {
                ResetTimeout(time_CONNECTINGREMOTE);
            }
            else if (this.State == ConnectState.WAITCONNECTINGREMOTE)
            {
                byte[] sendBuffer = CreateRequestConnectData(sendConnectBuffer);
                RemoteSendto(sendBuffer, sendBuffer.Length, -1, false);
                ResetTimeout(time_WAITCONNECTINGREMOTE);
            }
            else if (this.State == ConnectState.CONNECTED)
            {
                byte[] sendBuffer = CreateSyncData();
                RemoteSendto(sendBuffer, sendBuffer.Length, 0, false);
                ResetTimeout(time_CONNECTED);
            }
            else if (this.State == ConnectState.CONNECTIONEND)
            {
                try
                {
                    if (requestid != 0 && sock != null)
                    {
                        byte[] buf = CreateCloseConnectData();
                        RemoteSendto(buf, buf.Length, -1, false);
                    }
                }
                catch
                {
                    //pass
                }
                if (requestid == 0 || sock == null)
                {
                    ResetTimeout(0);
                    Close();
                }
                else
                {
                    ResetTimeout(time_CONNECTIONEND);
                }
            }
            else if (this.State == ConnectState.DISCONNECTING)
            {
                ResetTimeout(0);
                Close();
            }
            else
            {
                ResetTimeout(time_DEFAULT);
            }
        }
    }
}
