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

        private Calculagraph _timer;
        private int _timeOut = 20;

        /// <summary>
        /// 过期时间
        /// </summary>
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

        /// <summary>
        /// 重写GetWebRequest,添加WebRequest对象超时时间
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 1000 * Timeout;
            request.ReadWriteTimeout = 1000 * Timeout;
            return request;
        }

        /// <summary>
        /// 带过期计时的下载
        /// </summary>
        public void DownloadDataAsyncWithTimeout(Uri address)
        {
            if (_timer == null)
            {
                _timer = new Calculagraph(this);
                _timer.Timeout = Timeout;
                _timer.TimeOver += new TimeoutCaller(_timer_TimeOver);
                this.DownloadProgressChanged += new DownloadProgressChangedEventHandler(MyWebClient_DownloadProgressChanged);
            }

            DownloadDataAsync(address);
            _timer.Start();
        }

        /// <summary>
        /// WebClient下载过程事件，接收到数据时引发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MyWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //_timer.Reset();//重置计时器
        }

        /// <summary>
        /// 计时器过期
        /// </summary>
        /// <param name="userdata"></param>
        void _timer_TimeOver(object userdata)
        {
            this.CancelAsync();//取消下载
        }
    }
}
