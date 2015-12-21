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

        public void BeginDownload()
        {
            timeBeginDownload = DateTime.Now;
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
    }
}
