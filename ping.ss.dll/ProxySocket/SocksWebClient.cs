using System;
using System.Net;

namespace ping.ss.ProxySocket
{
    #region old
    /*
         public class Calculagraph
    {
        /// <summary>
        /// 时间到事件
        /// </summary>
        public event TimeoutCaller TimeOver;

        /// <summary>
        /// 开始时间
        /// </summary>
        private DateTime _startTime;
        private TimeSpan _timeout = new TimeSpan(0, 0, 10);
        private bool _hasStarted = false;
        object _userdata;

        /// <summary>
        /// 计时器构造方法
        /// </summary>
        /// <param name="userdata">计时结束时回调的用户数据</param>
        public Calculagraph(object userdata)
        {
            TimeOver += new TimeoutCaller(OnTimeOver);
            _userdata = userdata;
        }

        /// <summary>
        /// 超时退出
        /// </summary>
        /// <param name="userdata"></param>
        public virtual void OnTimeOver(object userdata)
        {
            Stop();
        }

        /// <summary>
        /// 过期时间(秒)
        /// </summary>
        public int Timeout
        {
            get
            {
                return _timeout.Seconds;
            }
            set
            {
                if (value <= 0)
                    return;
                _timeout = new TimeSpan(0, 0, value);
            }
        }

        /// <summary>
        /// 是否已经开始计时
        /// </summary>
        public bool HasStarted
        {
            get
            {
                return _hasStarted;
            }
        }

        /// <summary>
        /// 开始计时
        /// </summary>
        public void Start()
        {
            Reset();
            _hasStarted = true;
            System.Threading.Thread th = new System.Threading.Thread(WaitCall);
            th.IsBackground = true;
            th.Start();
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        public void Stop()
        {
            _hasStarted = false;
        }

        /// <summary>
        /// 检查是否过期
        /// </summary>
        /// <returns></returns>
        private bool checkTimeout()
        {
            return (DateTime.Now - _startTime).Seconds >= Timeout;
        }

        private void WaitCall()
        {
            try
            {
                //循环检测是否过期
                while (_hasStarted && !checkTimeout())
                {
                    System.Threading.Thread.Sleep(1000);
                }
                if (TimeOver != null)
                    TimeOver(_userdata);
            }
            catch (Exception)
            {
                Stop();
            }
        }
    }

    /// <summary>
    /// 过期时回调委托
    /// </summary>
    /// <param name="userdata"></param>
    public delegate void TimeoutCaller(object userdata);

    public class CNNWebClient : WebClient
    {

        private Calculagraph _timer;
        private int _timeOut = 5;

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
                if (value <= 0)
                    _timeOut = 5;
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
        public void DownloadFileAsyncWithTimeout(Uri address, string fileName, object userToken)
        {
            if (_timer == null)
            {
                _timer = new Calculagraph(this);
                _timer.Timeout = Timeout;
                _timer.TimeOver += new TimeoutCaller(_timer_TimeOver);
                this.DownloadProgressChanged += new DownloadProgressChangedEventHandler(CNNWebClient_DownloadProgressChanged);
            }

            DownloadFileAsync(address, fileName, userToken);
            _timer.Start();
        }

        /// <summary>
        /// WebClient下载过程事件，接收到数据时引发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CNNWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _timer.Reset();//重置计时器
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
     */
    #endregion

    public class SocksWebClient : WebClient
    {
        public IProxyDetails ProxyDetails { get; set; }
        public string UserAgent { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest result = null;

            if (ProxyDetails != null)
            {
                if (ProxyDetails.ProxyType == ProxyType.Proxy)
                {
                    result = (HttpWebRequest)WebRequest.Create(address);
                    result.Proxy = new WebProxy(ProxyDetails.FullProxyAddress);
                    if (!string.IsNullOrEmpty(UserAgent))
                        ((HttpWebRequest)result).UserAgent = UserAgent;
                }
                else if (ProxyDetails.ProxyType == ProxyType.Socks)
                {
                    result = SocksHttpWebRequest.Create(address);
                    result.Proxy = new WebProxy(ProxyDetails.FullProxyAddress);
                    //TODO: implement user and password

                }
                else if (ProxyDetails.ProxyType == ProxyType.None)
                {
                    result = (HttpWebRequest)WebRequest.Create(address);
                    if (!string.IsNullOrEmpty(UserAgent))
                        ((HttpWebRequest)result).UserAgent = UserAgent;
                }
            }
            else
            {
                result = (HttpWebRequest)WebRequest.Create(address);
                if (!string.IsNullOrEmpty(UserAgent))
                    ((HttpWebRequest)result).UserAgent = UserAgent;
            }
            return result;
        }
    }

    public interface IProxyDetails
    {
        ProxyType ProxyType { get; set; }
        /// <summary>
        /// adress and port
        /// </summary>
        string FullProxyAddress { get; set; }
        string ProxyAddress { get; set; }
        int ProxyPort { get; set; }
        string ProxyUserName { get; set; }
        string ProxyPassword { get; set; }
    }
    public class ProxyDetails : IProxyDetails
    {
        public ProxyType ProxyType { get; set; }
        /// <summary>
        /// adress and port
        /// </summary>
        public string FullProxyAddress { get; set; }
        public string ProxyAddress { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyUserName { get; set; }
        public string ProxyPassword { get; set; }

        public ProxyDetails()
        {
        }
        public ProxyDetails(int port)
        {
            FullProxyAddress = IPAddress.Loopback + ":" + port;
            ProxyAddress = IPAddress.Loopback.ToString();
            ProxyPort = port;
            ProxyType = ProxyType.Socks;
        }
    }

    public enum ProxyType
    {
        None = 0,
        Proxy = 1,
        Socks = 2
    }

    public static class EncodingHelper
    {
        public static string GetEncodingFromChunk(string chunk)
        {
            string charset = null;
            int charsetStart = chunk.IndexOf("charset=");
            int charsetEnd = -1;
            if (charsetStart != -1)
            {
                charsetEnd = chunk.IndexOfAny(new[] { ' ', '\"', ';', '\r', '\n' }, charsetStart);
                if (charsetEnd != -1)
                {
                    int start = charsetStart + 8;
                    charset = chunk.Substring(start, charsetEnd - start + 1);
                    charset = charset.TrimEnd(new Char[] { '>', '"', '\r', '\n' });
                }
            }
            return charset;
        }
    }
}
