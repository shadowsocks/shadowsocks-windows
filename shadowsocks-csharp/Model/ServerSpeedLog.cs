using System;
using System.Collections.Generic;
using System.Text;

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
        public long errorDecodeTimes;
        public long errorEmptyTimes;
        public long errorContinurousTimes;
        public long errorLogTimes;
        public long totalUploadBytes;
        public long totalDownloadBytes;
        public long totalDownloadRawBytes;
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
        private long errorDecodeTimes = 0;
        private long errorEmptyTimes = 0;
        private int lastError = 0;
        private long errorContinurousTimes = 0;
        private long transUpload = 0;
        private long transDownload = 0;
        private long transDownloadRaw = 0;
        private List<TransLog> transLog = null;
        private long maxTransDownload = 0;
        private List<int> connectTime = null;
        private int sumConnectTime = 0;
        private List<TransLog> speedLog = null;
        private LinkedList<int> errList = new LinkedList<int>();

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
                ret.errorDecodeTimes = errorDecodeTimes;
                ret.errorEmptyTimes = errorEmptyTimes;
                ret.errorLogTimes = errList.Count;
                ret.errorContinurousTimes = errorContinurousTimes;
                ret.totalUploadBytes = transUpload;
                ret.totalDownloadBytes = transDownload;
                ret.totalDownloadRawBytes = transDownloadRaw;
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
                    return errorDecodeTimes;
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
                errorDecodeTimes = 0;
                errorEmptyTimes = 0;
                errList.Clear();
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
                errorDecodeTimes = 0;
                errorEmptyTimes = 0;
                errList.Clear();
                lastError = 0;
                errorContinurousTimes = 0;
                transUpload = 0;
                transDownload = 0;
                transDownloadRaw = 0;
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
        protected void FixErrList()
        {
            while (errList.Count > 100)
            {
                int errCode = errList.First.Value;
                errList.RemoveFirst();
                if (errCode == 1)
                {
                    errorConnectTimes -= 1;
                }
                else if (errCode == 2)
                {
                    errorTimeoutTimes -= 1;
                }
                else if (errCode == 3)
                {
                    errorDecodeTimes -= 1;
                }
                else if (errCode == 4)
                {
                    errorEmptyTimes -= 1;
                }
            }
        }
        public void AddNoErrorTimes()
        {
            lock (this)
            {
                errList.AddLast(0);
                errorEmptyTimes = 0;
                FixErrList();
            }
        }
        public void AddErrorTimes()
        {
            lock (this)
            {
                errorConnectTimes += 1;
                errorContinurousTimes += 1;
                errList.AddLast(1);
                if (lastError == 1)
                {
                }
                else
                {
                    lastError = 1;
                    //errorContinurousTimes = 0;
                }
                FixErrList();
            }
        }
        public void AddTimeoutTimes()
        {
            lock (this)
            {
                errorTimeoutTimes += 1;
                errorContinurousTimes += 1;
                errList.AddLast(2);
                if (lastError == 2)
                {
                }
                else
                {
                    lastError = 2;
                    //errorContinurousTimes = 0;
                }
                FixErrList();
            }
        }
        public void AddErrorDecodeTimes()
        {
            lock (this)
            {
                errorDecodeTimes += 1;
                errorContinurousTimes += 1;
                errList.AddLast(0);
                if (lastError == 3)
                {
                }
                else
                {
                    lastError = 3;
                    //errorContinurousTimes = 0;
                }
                FixErrList();
            }
        }
        public void AddErrorEmptyTimes()
        {
            lock (this)
            {
                errorEmptyTimes += 1;
                errorContinurousTimes += 1;
                errList.AddLast(0);
                if (lastError == 4)
                {
                }
                else
                {
                    lastError = 4;
                    //errorContinurousTimes = 0;
                }
                FixErrList();
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
        public void AddDownloadRawBytes(long bytes)
        {
            lock (this)
            {
                transDownloadRaw += bytes;
            }
        }
        public void ResetErrorDecodeTimes()
        {
            lock (this)
            {
                lastError = 0;
                errorDecodeTimes = 0;
                errorEmptyTimes = 0;
                errorContinurousTimes = 0;
            }
        }
        public void ResetContinurousTimes()
        {
            lock (this)
            {
                lastError = 0;
                errorEmptyTimes = 0;
                errorContinurousTimes = 0;
            }
        }
        public void ResetEmptyTimes()
        {
            lock (this)
            {
                errorEmptyTimes = 0;
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
}
