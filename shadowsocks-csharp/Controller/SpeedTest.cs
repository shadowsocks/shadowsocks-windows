using System;
using System.Collections.Generic;
using System.Text;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{

    class SpeedTester
    {
        public DateTime timeConnectBegin;
        public DateTime timeConnectEnd;
        public DateTime timeBeginUpload;
        public DateTime timeBeginDownload;
        public long sizeUpload = 0;
        public long sizeDownload = 0;
        public long sizeRecv = 0;
        private List<TransLog> sizeDownloadList = new List<TransLog>();

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
            if (sizeDownloadList.Count == 2)
                sizeDownloadList[1] = new TransLog(size, DateTime.Now);
            else
                sizeDownloadList.Add(new TransLog(size, DateTime.Now));
            sizeDownload += size;
        }

        public void AddRecvSize(int size)
        {
            sizeRecv += size;
        }

        public void AddUploadSize(int size)
        {
            sizeUpload += size;
        }

        public long GetAvgDownloadSpeed()
        {
            if (sizeDownloadList == null || sizeDownloadList.Count < 2 || (sizeDownloadList[sizeDownloadList.Count - 1].recvTime - sizeDownloadList[0].recvTime).TotalSeconds <= 0.001)
                return 0;
            return (long)((sizeDownload - sizeDownloadList[0].size) / (sizeDownloadList[sizeDownloadList.Count - 1].recvTime - sizeDownloadList[0].recvTime).TotalSeconds);
        }

        public int GetActionType()
        {
            int type = 0;
            if (sizeDownload > 1024 * 1024 * 1)
            {
                type |= 1;
            }
            if (sizeUpload > 1024 * 1024 * 1)
            {
                type |= 2;
            }
            double time = (DateTime.Now - timeConnectEnd).TotalSeconds;
            if (time > 5 && (sizeDownload + sizeUpload) / time > 1024 * 16)
            {
                type |= 4;
            }
            return type;
        }
    }
}
