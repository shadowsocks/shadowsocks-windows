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
	/// Base Resource Record class for objects returned in 
	/// answers, authorities and additional record DNS responses. 
	/// </summary>
	public class ResourceRecord
	{
		public string Name; 
		public Types Type; 
		public Classes Class; 
		public int TimeToLive; 
		public string RText; 

		public ResourceRecord()
		{
		}

		public ResourceRecord(string _Name, Types _Type, Classes _Class, int _TimeToLive)
		{
			this.Name = _Name; 
			this.Type = _Type; 
			this.Class = _Class; 
			this.TimeToLive = _TimeToLive; 
		}

		public ResourceRecord(string _Name, Types _Type, Classes _Class, int _TimeToLive, string _RText)
		{
			this.Name = _Name; 
			this.Type = _Type; 
			this.Class = _Class; 
			this.TimeToLive = _TimeToLive; 
			this.RText = _RText; 
		}

		public override string ToString()
		{
			
			StringBuilder sb = new StringBuilder(); 
			sb.Append("Name=" + Name + "&Type=" + Type + "&Class=" + Class + "&TTL="+TimeToLive); 
			//TODO: Return TTL as minutes? 
			//TimeSpan timeSpan = new TimeSpan(0, 0, 0, TimeToLive, 0);

			return sb.ToString();
		}
	}

}