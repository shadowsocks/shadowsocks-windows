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
using System.Collections;
using System.Text;

namespace OpenDNS
{
	/// <summary>
	/// The Collection Class inherits from ArrayList.  It has its own implemenation 
	/// of Sort based on the sortable fields.
	/// </summary>
	public class ResourceRecordCollection : ArrayList
	{
		public enum SortFields
		{
			Name,
			TTL
		}

		public void Sort(SortFields sortField, bool isAscending)
		{
			switch (sortField) 
			{
				case SortFields.Name:
					base.Sort(new NameComparer());
					break;
				case SortFields.TTL:
					base.Sort(new TTLComparer());
					break;
			}

			if (!isAscending) base.Reverse();
		}

		private sealed class NameComparer : IComparer 
		{
			public int Compare(object x, object y)
			{
				ResourceRecord first = (ResourceRecord) x;
				ResourceRecord second = (ResourceRecord) y;
				return first.Name.CompareTo(second.Name);
			}
		}

		private sealed class TTLComparer : IComparer 
		{
			public int Compare(object x, object y)
			{
				ResourceRecord first = (ResourceRecord) x;
				ResourceRecord second = (ResourceRecord) y;
				return first.TimeToLive.CompareTo(second.TimeToLive);
			}
		}
	}
}

