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
using System.Net;

namespace OpenDNS
{
	/// <summary>
	/// Address Resource Record
	/// </summary>
	public class Address : ResourceRecord
	{
		public string ResourceAddress; 
		private IPAddress _IP; 

		public IPAddress IP
		{
			get 
			{ 
				if (_IP == null) _IP = IPAddress.Parse(ResourceAddress); 
				return _IP; 
			}
		}

		public Address(string _Name, Types _Type, Classes _Class, int _TimeToLive, string _ResourceAddress):base(_Name, _Type, _Class, _TimeToLive)
		{
			ResourceAddress = _ResourceAddress; 
			RText = _ResourceAddress;
		}
	}
}