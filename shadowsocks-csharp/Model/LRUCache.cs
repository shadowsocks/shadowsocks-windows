using System;
using System.Collections.Generic;
using System.Text;
using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    public class LRUCache<K, V>
    {
        protected Dictionary<K, V> _store = new Dictionary<K, V>();
        protected Dictionary<K, DateTime> _key_2_time = new Dictionary<K, DateTime>();
        protected Dictionary<DateTime, K> _time_2_key = new Dictionary<DateTime, K>();
        protected object _lock = new object();
        protected int _sweep_time;

        public LRUCache(int sweep_time = 60 * 60)
        {
            _sweep_time = sweep_time;
        }

        public void SetTimeout(int time)
        {
            _sweep_time = time;
        }

        public bool isTimeout(K key)
        {
            lock (_lock)
            {
                if ((DateTime.Now - _key_2_time[key]).TotalSeconds > _sweep_time)
                {
                    return true;
                }
                return false;
            }
        }

        public bool ContainsKey(K key)
        {
            lock (_lock)
            {
                return _store.ContainsKey(key);
            }
        }

        public V Get(K key)
        {
            lock (_lock)
            {
                if (_store.ContainsKey(key))
                {
                    DateTime t = _key_2_time[key];
                    _key_2_time.Remove(key);
                    _time_2_key.Remove(t);
                    t = DateTime.Now;
                    while (_time_2_key.ContainsKey(t))
                    {
                        t = t.AddTicks(1);
                    }
                    _time_2_key[t] = key;
                    _key_2_time[key] = t;
                    return _store[key];
                }
                return default(V);
            }
        }

        public V Set(K key, V val)
        {
            lock (_lock)
            {
                DateTime t;
                if (_store.ContainsKey(key))
                {
                    t = _key_2_time[key];
                    _key_2_time.Remove(key);
                    _time_2_key.Remove(t);
                }
                t = DateTime.Now;
                while (_time_2_key.ContainsKey(t))
                {
                    t = t.AddTicks(1);
                }
                _time_2_key[t] = key;
                _key_2_time[key] = t;
                _store[key] = val;
                return val;
            }
        }

        public void Del(K key)
        {
            lock (_lock)
            {
                DateTime t;
                if (_store.ContainsKey(key))
                {
                    t = _key_2_time[key];
                    _key_2_time.Remove(key);
                    _time_2_key.Remove(t);
                    _store.Remove(key);
                }
            }
        }

        public void Sweep()
        {
            lock (_lock)
            {
                DateTime now = DateTime.Now;
                int sweep = 0;
                for (int i = 0; i < 100; ++i)
                {
                    bool finish = false;
                    foreach (KeyValuePair<DateTime, K> p in _time_2_key)
                    {
                        if ((now - p.Key).TotalSeconds < _sweep_time)
                        {
                            finish = true;
                            break;
                        }
                        _key_2_time.Remove(p.Value);
                        _time_2_key.Remove(p.Key);
                        _store.Remove(p.Value);
                        Logging.Debug("sweep [" + p.Key.ToString() + "]: " + p.Value.ToString());
                        sweep += 1;
                        break;
                    }
                    if (finish)
                        break;
                }
                if (sweep > 0)
                {
                    Logging.Debug("sweep " + sweep.ToString() + " items");
                }
            }
        }
    }
}
