using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Shadowsocks.Model
{
    public class IPRangeSet
    {
        private const string APNIC_FILENAME = "delegated-apnic.txt";
        private uint[] _set;

        public IPRangeSet()
        {
            _set = new uint[256 * 256 * 8];
        }

        public void Insert(uint begin, uint size)
        {
            begin /= 256;
            size /= 256;
            for (uint i = begin; i < begin + size; ++i)
            {
                uint pos = i / 32;
                int mv = (int)(i & 31);
                _set[pos] |= (byte)(1u << mv);
            }
        }

        public bool isIn(uint ip)
        {
            ip /= 256;
            uint pos = ip / 32;
            int mv = (int)(ip & 31);
            return (_set[pos] & (byte)(1u << mv)) != 0;
        }

        public bool IsInIPRange(IPAddress addr)
        {
            byte[] bytes_addr = addr.GetAddressBytes();
            Array.Reverse(bytes_addr);
            return isIn(BitConverter.ToUInt32(bytes_addr, 0));
        }

        public bool LoadApnic(string zone)
        {
            if (File.Exists(APNIC_FILENAME))
            {
                try
                {
                    using (StreamReader stream = File.OpenText(APNIC_FILENAME))
                    {
                        while (true)
                        {
                            string line = stream.ReadLine();
                            if (line == null)
                                break;
                            string[] parts = line.Split('|');
                            if (parts.Length < 7)
                                continue;
                            if (parts[0] != "apnic" || parts[1] != zone || parts[2] != "ipv4")
                                continue;
                            IPAddress addr;
                            IPAddress.TryParse(parts[3], out addr);
                            byte[] bytes_addr = addr.GetAddressBytes();
                            Array.Reverse(bytes_addr);
                            uint start = BitConverter.ToUInt32(bytes_addr, 0);
                            uint size = UInt32.Parse(parts[4]);
                            Insert(start, size);
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public void Reverse()
        {
            for (uint i = 0; i < _set.Length; ++i)
            {
                _set[i] = ~_set[i];
            }
        }
    }
}
