using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Controller.Service
{
    public class SpeedTestWebClient : WebClient
    {
        private SpeedTestTimer _timer;
        private int _timeOut = 20;
        
        public int Timeout
        {
            get
            {
                return _timeOut;
            }
            set
            {
                _timeOut = value;
            }
        }
        
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 1000 * Timeout;
            request.ReadWriteTimeout = 1000 * Timeout;
            return request;
        }
        
        public void DownloadDataAsyncWithTimeout(Uri address)
        {
            if (_timer == null)
            {
                _timer = new SpeedTestTimer(this);
                _timer.Timeout = Timeout;
                _timer.TimeOver += new TimeoutCaller(_timer_TimeOver);
                this.DownloadProgressChanged += new DownloadProgressChangedEventHandler(MyWebClient_DownloadProgressChanged);
            }

            DownloadDataAsync(address);
            _timer.Start();
        }
        
        void MyWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //_timer.Reset();
        }
        
        void _timer_TimeOver(object userdata)
        {
            this.CancelAsync();
        }
    }
}
