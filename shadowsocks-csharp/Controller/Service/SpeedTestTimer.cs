using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Controller.Service
{
    public class SpeedTestTimer
    {
        public event TimeoutCaller TimeOver;
        
        private DateTime _startTime;
        private TimeSpan _timeout = new TimeSpan(0, 0, 10);
        private bool _hasStarted = false;
        object _userdata;
        
        public SpeedTestTimer(object userdata)
        {
            TimeOver += new TimeoutCaller(OnTimeOver);
            _userdata = userdata;
        }
        
        public virtual void OnTimeOver(object userdata)
        {
            Stop();
        }
        
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
        
        public bool HasStarted
        {
            get
            {
                return _hasStarted;
            }
        }
        
        public void Start()
        {
            Reset();
            _hasStarted = true;
            Thread th = new Thread(WaitCall);
            th.IsBackground = true;
            th.Start();
        }
        
        public void Reset()
        {
            _startTime = DateTime.Now;
        }
        
        public void Stop()
        {
            _hasStarted = false;
        }
        
        private bool checkTimeout()
        {
            return (DateTime.Now - _startTime).Seconds >= Timeout;
        }

        private void WaitCall()
        {
            try
            {
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
    
    public delegate void TimeoutCaller(object userdata);
}
