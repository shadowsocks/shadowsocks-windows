using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Shadowsocks._3rd
{
    ///<summary> 
    /// QQWry 的摘要说明。 
    ///</summary> 
    public class QQWry
    {
        #region Properties
        ///<summary> 
        ///第一种模式 
        ///</summary> 
        private const byte REDIRECT_MODE_1 = 0x01;

        ///<summary> 
        ///第二种模式 
        ///</summary> 
        private const byte REDIRECT_MODE_2 = 0x02;

        ///<summary> 
        ///每条记录长度 
        ///</summary> 
        private const int IP_RECORD_LENGTH = 7;

        ///<summary> 
        ///文件对象 
        ///</summary> 
        private FileStream ipFile;

        private const string unCountry = "未知国家";
        private const string unArea = "未知地区";

        ///<summary> 
        ///索引开始位置 
        ///</summary> 
        private long ipBegin;

        ///<summary> 
        ///索引结束位置 
        ///</summary> 
        private long ipEnd;

        ///<summary> 
        /// IP对象 
        ///</summary> 
        private IPLocation loc;

        ///<summary> 
        ///存储文本内容 
        ///</summary> 
        private byte[] buf;

        ///<summary> 
        ///存储3字节 
        ///</summary> 
        private byte[] b3;

        ///<summary> 
        ///存储4字节IP地址 
        ///</summary> 
        private byte[] b4;
        #endregion

        #region 构造函数

        ///<summary> 
        ///构造函数 
        ///</summary> 
        ///<param name="ipfile">IP数据库文件绝对路径</param> 
        public QQWry(string ipfile)
        {

            buf = new byte[100];
            b3 = new byte[3];
            b4 = new byte[4];
            try
            {
                ipFile = new FileStream(ipfile, FileMode.Open);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            ipBegin = readLong4(0);
            ipEnd = readLong4(4);
            loc = new IPLocation();
        }
        #endregion

        #region 根据IP地址搜索

        ///<summary> 
        ///搜索IP地址搜索 
        ///</summary> 
        ///<param name="ip"></param> 
        ///<returns></returns> 
        public IPLocation SearchIPLocation(string ip)
        {
            //将字符IP转换为字节 
            string[] ipSp = ip.Split('.');
            if (ipSp.Length != 4)
            {
                throw new ArgumentOutOfRangeException("不是合法的IP地址!");
            }
            byte[] IP = new byte[4];
            for (int i = 0; i < IP.Length; i++)
            {
                IP[i] = (byte) (Int32.Parse(ipSp[i]) & 0xFF);
            }

            IPLocation local = null;
            long offset = locateIP(IP);

            if (offset != -1)
            {
                local = getIPLocation(offset);
            }

            if (local == null)
            {
                local = new IPLocation();
                local.area = unArea;
                local.country = unCountry;
            }
            return local;
        }
        #endregion

        #region 取得具体信息 
        ///<summary> 
        ///取得具体信息 
        ///</summary> 
        ///<param name="offset"></param> 
        ///<returns></returns> 
        private IPLocation getIPLocation(long offset)
        {
            ipFile.Position = offset + 4;
            //读取第一个字节判断是否是标志字节 
            byte one = (byte) ipFile.ReadByte();
            if (one == REDIRECT_MODE_1)
            {
                //第一种模式 
                //读取国家偏移 
                long countryOffset = readLong3();
                //转至偏移处 
                ipFile.Position = countryOffset;
                //再次检查标志字节 
                byte b = (byte) ipFile.ReadByte();
                if (b == REDIRECT_MODE_2)
                {
                    loc.country = readString(readLong3());
                    ipFile.Position = countryOffset + 4;
                }
                else
                    loc.country = readString(countryOffset);

                //读取地区标志 
                loc.area = readArea(ipFile.Position);

            }
            else if (one == REDIRECT_MODE_2)
            {
                //第二种模式 
                loc.country = readString(readLong3());
                loc.area = readArea(offset + 8);
            }
            else
            {
                //普通模式 
                loc.country = readString(--ipFile.Position);
                loc.area = readString(ipFile.Position);
            }
            return loc;
        }
        #endregion

        #region 取得地区信息
        ///<summary> 
        ///读取地区名称 
        ///</summary> 
        ///<param name="offset"></param> 
        ///<returns></returns> 
        private string readArea(long offset)
        {
            ipFile.Position = offset;
            byte one = (byte) ipFile.ReadByte();
            if (one == REDIRECT_MODE_1 || one == REDIRECT_MODE_2)
            {
                long areaOffset = readLong3(offset + 1);
                if (areaOffset == 0)
                    return unArea;
                else
                {
                    return readString(areaOffset);
                }
            }
            else
            {
                return readString(offset);
            }
        }

        #endregion

        #region 读取字符串

        ///<summary> 
        ///读取字符串 
        ///</summary> 
        ///<param name="offset"></param> 
        ///<returns></returns> 

        private string readString(long offset)
        {
            ipFile.Position = offset;
            int i = 0;
            for (i = 0, buf[i] = (byte) ipFile.ReadByte(); buf[i] != (byte) (0); buf[++i] = (byte) ipFile.ReadByte()) ;

            if (i > 0)
                return Encoding.Default.GetString(buf, 0, i);
            else
                return "";
        }
        #endregion

        #region 查找IP地址所在的绝对偏移量

        /**/

        ///<summary> 
        ///查找IP地址所在的绝对偏移量 
        ///</summary> 
        ///<param name="ip"></param> 
        ///<returns></returns> 

        private long locateIP(byte[] ip)
        {
            long m = 0;
            int r;

            //比较第一个IP项 
            readIP(ipBegin, b4);
            r = compareIP(ip, b4);
            if (r == 0)
                return ipBegin;
            else if (r < 0)
                return -1;
            //开始二分搜索 
            for (long i = ipBegin, j = ipEnd; i < j;)
            {
                m = this.getMiddleOffset(i, j);
                readIP(m, b4);
                r = compareIP(ip, b4);
                if (r > 0)
                    i = m;
                else if (r < 0)
                {
                    if (m == j)
                    {
                        j -= IP_RECORD_LENGTH;
                        m = j;
                    }
                    else
                    {
                        j = m;
                    }
                }
                else
                    return readLong3(m + 4);
            }
            m = readLong3(m + 4);
            readIP(m, b4);
            r = compareIP(ip, b4);
            if (r <= 0)
                return m;
            else
                return -1;
        }

        #endregion

        #region 读出4字节的IP地址

        /**/

        ///<summary> 
        ///从当前位置读取四字节,此四字节是IP地址 
        ///</summary> 
        ///<param name="offset"></param> 
        ///<param name="ip"></param> 

        private void readIP(long offset, byte[] ip)
        {
            ipFile.Position = offset;
            ipFile.Read(ip, 0, ip.Length);
            byte tmp = ip[0];
            ip[0] = ip[3];
            ip[3] = tmp;
            tmp = ip[1];
            ip[1] = ip[2];
            ip[2] = tmp;
        }

        #endregion

        #region 比较IP地址是否相同

        /**/

        ///<summary> 
        ///比较IP地址是否相同 
        ///</summary> 
        ///<param name="ip"></param> 
        ///<param name="beginIP"></param> 
        ///<returns>0:相等,1:ip大于beginIP,-1:小于</returns> 

        private int compareIP(byte[] ip, byte[] beginIP)
        {
            for (int i = 0; i < 4; i++)
            {
                int r = compareByte(ip[i], beginIP[i]);
                if (r != 0)
                    return r;
            }
            return 0;
        }

        #endregion

        #region 比较两个字节是否相等

        /**/

        ///<summary> 
        ///比较两个字节是否相等 
        ///</summary> 
        ///<param name="bsrc"></param> 
        ///<param name="bdst"></param> 
        ///<returns></returns> 

        private int compareByte(byte bsrc, byte bdst)
        {
            if ((bsrc & 0xFF) > (bdst & 0xFF))
                return 1;
            else if ((bsrc ^ bdst) == 0)
                return 0;
            else
                return -1;
        }

        #endregion

        #region 根据当前位置读取4字节

        /**/

        ///<summary> 
        ///从当前位置读取4字节,转换为长整型 
        ///</summary> 
        ///<param name="offset"></param> 
        ///<returns></returns> 

        private long readLong4(long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            ret |= ((ipFile.ReadByte() << 24) & 0xFF000000);
            return ret;
        }

        #endregion

        #region 根据当前位置,读取3字节

        /**/

        ///<summary> 
        ///根据当前位置,读取3字节 
        ///</summary> 
        ///<param name="offset"></param> 
        ///<returns></returns> 

        private long readLong3(long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
        }

        #endregion

        #region 从当前位置读取3字节

        /**/

        ///<summary> 
        ///从当前位置读取3字节 
        ///</summary> 
        ///<returns></returns> 

        private long readLong3()
        {
            long ret = 0;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
        }

        #endregion

        #region 取得begin和end之间的偏移量

        /**/

        ///<summary> 
        ///取得begin和end中间的偏移 
        ///</summary> 
        ///<param name="begin"></param> 
        ///<param name="end"></param> 
        ///<returns></returns>         
        private long getMiddleOffset(long begin, long end)
        {
            long records = (end - begin)/IP_RECORD_LENGTH;
            records >>= 1;
            if (records == 0)
                records = 1;
            return begin + records*IP_RECORD_LENGTH;
        }

        #endregion
    }

    public class IPLocation
    {
        public String country;
        public String area;

        public IPLocation()
        {
            country = area = "";
        }

        public IPLocation getCopy()
        {
            IPLocation ret = new IPLocation();
            ret.country = country;
            ret.area = area;
            return ret;
        }
    } 

#region Old
    /*
    /// <summary>  
    /// 存储地区的结构  
    /// </summary>  
    public struct stLocation
    {
        /// <summary>  
        /// 未使用  
        /// </summary>  
        public string Ip;

        /// <summary>  
        /// 国家名  
        /// </summary>  
        public string Contry;

        /// <summary>  
        /// 城市名  
        /// </summary>  
        public string City;
    }


    /// <summary>  
    /// 纯真IP数据库查询辅助类  
    /// </summary>  
    public static class QqwryHelper
    {
        #region 成员变量

        private const byte REDIRECT_MODE_1 = 0x01;//名称存储模式一  
        private const byte REDIRECT_MODE_2 = 0x02;//名称存储模式二  
        private const int IP_RECORD_LENGTH = 7; //每条索引的长度  

        private static long beginIndex = 0;//索引开始  
        private static long endIndex = 0;//索引结束  

        private static stLocation loc = new stLocation() { City = "未知城市", Contry = "未知国家" };

        private static Stream fs;

        #endregion

        #region 私有成员函数

        /// <summary>  
        /// 在索引区查找指定IP对应的记录区地址  
        /// </summary>  
        /// <param name="_ip">字节型IP</param>  
        /// <returns></returns>  
        private static long SearchIpIndex(byte[] _ip)
        {
            long index = 0;

            byte[] nextIp = new byte[4];

            ReadIp(beginIndex, ref nextIp);

            int flag = CompareIp(_ip, nextIp);
            if (flag == 0) return beginIndex;
            else if (flag < 0) return -1;

            for (long i = beginIndex, j = endIndex; i < j; )
            {
                index = GetMiddleOffset(i, j);

                ReadIp(index, ref nextIp);
                flag = CompareIp(_ip, nextIp);

                if (flag == 0) return ReadLong(index + 4, 3);
                else if (flag > 0) i = index;
                else if (flag < 0)
                {
                    if (index == j)
                    {
                        j -= IP_RECORD_LENGTH;
                        index = j;
                    }
                    else
                    {
                        j = index;
                    }
                }
            }

            index = ReadLong(index + 4, 3);
            ReadIp(index, ref nextIp);

            flag = CompareIp(_ip, nextIp);
            if (flag <= 0) return index;
            else return -1;
        }

        /// <summary>  
        /// 获取两个索引的中间位置  
        /// </summary>  
        /// <param name="begin">索引1</param>  
        /// <param name="end">索引2</param>  
        /// <returns></returns>  
        private static long GetMiddleOffset(long begin, long end)
        {
            long records = (end - begin) / IP_RECORD_LENGTH;
            records >>= 1;
            if (records == 0) records = 1;
            return begin + records * IP_RECORD_LENGTH;
        }

        /// <summary>  
        /// 读取记录区的地区名称  
        /// </summary>  
        /// <param name="offset">位置</param>  
        /// <returns></returns>  
        private static string ReadString(long offset)
        {
            fs.Position = offset;

            byte b = (byte)fs.ReadByte();
            if (b == REDIRECT_MODE_1 || b == REDIRECT_MODE_2)
            {
                long areaOffset = ReadLong(offset + 1, 3);
                if (areaOffset == 0)
                    return "未知地区";

                else fs.Position = areaOffset;
            }
            else
            {
                fs.Position = offset;
            }

            List<byte> buf = new List<byte>();

            int i = 0;
            for (i = 0, buf.Add((byte)fs.ReadByte()); buf[i] != (byte)(0); ++i, buf.Add((byte)fs.ReadByte())) ;

            if (i > 0) return Encoding.Default.GetString(buf.ToArray(), 0, i);
            else return "";
        }

        /// <summary>  
        /// 从自定位置读取指定长度的字节，并转换为big-endian字节序(数据源文件为little-endian字节序)  
        /// </summary>  
        /// <param name="offset">开始读取位置</param>  
        /// <param name="length">读取长度</param>  
        /// <returns></returns>  
        private static long ReadLong(long offset, int length)
        {
            long ret = 0;
            fs.Position = offset;
            for (int i = 0; i < length; i++)
            {
                ret |= ((fs.ReadByte() << (i * 8)) & (0xFF * ((int)Math.Pow(16, i * 2))));
            }

            return ret;
        }

        /// <summary>  
        /// 从指定位置处读取一个IP  
        /// </summary>  
        /// <param name="offset">指定的位置</param>  
        /// <param name="_buffIp">保存IP的缓存区</param>  
        private static void ReadIp(long offset, ref byte[] _buffIp)
        {
            fs.Position = offset;
            fs.Read(_buffIp, 0, _buffIp.Length);

            for (int i = 0; i < _buffIp.Length / 2; i++)
            {
                byte temp = _buffIp[i];
                _buffIp[i] = _buffIp[_buffIp.Length - i - 1];
                _buffIp[_buffIp.Length - i - 1] = temp;
            }
        }

        /// <summary>  
        /// 比较两个IP是否相等，1:IP1大于IP2，-1：IP1小于IP2，0：IP1=IP2  
        /// </summary>  
        /// <param name="_buffIp1">IP1</param>  
        /// <param name="_buffIp2">IP2</param>  
        /// <returns></returns>  
        private static int CompareIp(byte[] _buffIp1, byte[] _buffIp2)
        {
            if (_buffIp1.Length > 4 || _buffIp2.Length > 4) throw new Exception("指定的IP无效。");

            for (int i = 0; i < 4; i++)
            {
                if ((_buffIp1[i] & 0xFF) > (_buffIp2[i] & 0xFF)) return 1;
                else if ((_buffIp1[i] & 0xFF) < (_buffIp2[i] & 0xFF)) return -1;
            }

            return 0;
        }

        /// <summary>  
        /// 从指定的地址获取区域名称  
        /// </summary>  
        /// <param name="offset"></param>  
        private static void GetAreaName(long offset)
        {
            fs.Position = offset + 4;
            long flag = fs.ReadByte();
            long contryIndex = 0;
            if (flag == REDIRECT_MODE_1)
            {
                contryIndex = ReadLong(fs.Position, 3);
                fs.Position = contryIndex;

                flag = fs.ReadByte();

                if (flag == REDIRECT_MODE_2)    //是否仍然为重定向  
                {
                    loc.Contry = ReadString(ReadLong(fs.Position, 3));
                    fs.Position = contryIndex + 4;
                }
                else
                {
                    loc.Contry = ReadString(contryIndex);
                }
                loc.City = ReadString(fs.Position);
            }
            else if (flag == REDIRECT_MODE_2)
            {
                contryIndex = ReadLong(fs.Position, 3);
                loc.Contry = ReadString(contryIndex);
                loc.City = ReadString(contryIndex + 3);
            }
            else
            {
                loc.Contry = ReadString(offset + 4);
                loc.City = ReadString(fs.Position);
            }
        }

        #endregion

        #region 公有成员函数

        /// <summary>  
        /// 加载数据库文件到缓存  
        /// </summary>  
        /// <param name="path">数据库文件地址</param>  
        /// <returns></returns>  
        public static void Init(string path)
        {
            if (fs != null) return;
            var bt = File.ReadAllBytes(path);
            fs = new MemoryStream(bt);
        }

        /// <summary>  
        /// 根据IP获取区域名  
        /// </summary>  
        /// <param name="ip">指定的IP</param>  
        /// <returns></returns>  
        public static stLocation GetLocation(string ip)
        {
            IPAddress ipAddress = null;
            if (!IPAddress.TryParse(ip, out ipAddress)) throw new Exception("无效的IP地址。");

            byte[] buff_local_ip = ipAddress.GetAddressBytes();

            beginIndex = ReadLong(0, 4);
            endIndex = ReadLong(4, 4);

            long offset = SearchIpIndex(buff_local_ip);
            if (offset != -1)
            {
                GetAreaName(offset);
            }

            loc.Contry = loc.Contry.Trim();
            loc.City = loc.City.Trim().Replace("CZ88.NET", "");

            return loc;
        }

        /// <summary>  
        /// 释放资源  
        /// </summary>  
        public static void Dispose()
        {
            fs.Dispose();
        }

        #endregion
    }  */
#endregion
}
