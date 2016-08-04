/* 
 * Author: Ruy Delgado <ruydelgado@gmail.com>
 * Title: OpenDNS
 * Description: DNS Client Library 
 * Revision: 1.0
 * Last Modified: 2005.01.28
 * Created On: 2005.01.28
 * 
 * Note: Based on DnsLite by Jaimon Mathew
 * */

using System;
using System.Text;

namespace OpenDNS
{
	/// <summary>
	/// Start of Authority Resource Record
	/// </summary>
	public class SOA : ResourceRecord
	{
		public string Server;
		public string Email;
		public long Serial;
		public long Refresh;
		public long Retry;
		public long Expire;
		public long Minimum;
		
		public SOA(string _Name, Types _Type, Classes _Class, int _TimeToLive, string _Server, string _Email, long _Serial, long _Refresh, long _Retry, long _Expire, long _Minimum):base(_Name, _Type, _Class, _TimeToLive)
		{
			Server = _Server;
			Email = _Email;
			Serial = _Serial;
			Refresh = _Refresh;
			Retry = _Retry;
			Expire = _Expire;
			Minimum = _Minimum;
		}

	}
}