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

namespace OpenDNS
{
    /// <summary>
    /// Query Result/Response Codes from server
    /// </summary>
    public enum ResponseCodes : int
    {
        NoError = 0,
        FormatError = 1,
        ServerFailure = 2,
        NameError = 3,
        NotImplemented = 4,
        Refused = 5,
        Reserved = 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15
    }


    /// <summary>
    /// DNS Resource Record Types
    /// </summary>
    public enum Types : int
    {
        A = 1,
        AAAA = 28,
        NS = 2,
        CNAME = 5,
        SOA = 6,
        MB = 7,
        MG = 8,
        MR = 9,
        NULL = 10,
        WKS = 11,
        PTR = 12,
        HINFO = 13,
        MINFO = 14,
        MX = 15,
        TXT = 16,
        ANY = 255
    }


    /// <summary>
    /// Query Class or Scope
    /// </summary>
    public enum Classes : int
    {
        IN = 1,
        CS = 2,
        CH = 3,
        HS = 4,
        ANY = 255
    }
}
