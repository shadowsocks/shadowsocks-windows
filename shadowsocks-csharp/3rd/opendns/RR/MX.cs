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
	/// MailExchange Resource Record
	/// </summary>
	public class MX : ResourceRecord
	{
		public int Preference;
		public string Exchange;

		public MX(string _Name, Types _Type, Classes _Class, int _TimeToLive, int _Preference, string _Exchange):base(_Name, _Type, _Class, _TimeToLive)
		{
			Preference = _Preference;
			Exchange = _Exchange; 
		}
	}
}