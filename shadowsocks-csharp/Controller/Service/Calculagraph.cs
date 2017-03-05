using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Controller.Service
{
    /// <summary>
    /// https://www.cnblogs.com/heros/archive/2009/07/06/1517997.html
    /// </summary>
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
            Thread th = new Thread(WaitCall);
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
                    Thread.Sleep(1000);
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
}
