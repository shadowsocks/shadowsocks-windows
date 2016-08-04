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
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace OpenDNS
{
    /// <summary>
    /// Response object as result of a dns query message. 
    /// Will be null unless query succesfull. 
    /// </summary>
    public class DnsResponse
    {
        private int _QueryID;

        //Property Internals
        private bool _AuthorativeAnswer;
        private bool _IsTruncated;
        private bool _RecursionDesired;
        private bool _RecursionAvailable;
        private ResponseCodes _ResponseCode;

        private ResourceRecordCollection _ResourceRecords;
        private ResourceRecordCollection _Answers;
        private ResourceRecordCollection _Authorities;
        private ResourceRecordCollection _AdditionalRecords;

        //Read Only Public Properties
        public int QueryID
        {
            get { return _QueryID; }
        }

        public bool AuthorativeAnswer
        {
            get { return _AuthorativeAnswer; }
        }

        public bool IsTruncated
        {
            get { return _IsTruncated; }
        }

        public bool RecursionRequested
        {
            get { return _RecursionDesired; }
        }

        public bool RecursionAvailable
        {
            get { return _RecursionAvailable; }
        }

        public ResponseCodes ResponseCode
        {
            get { return _ResponseCode; }
        }

        public ResourceRecordCollection Answers
        {
            get { return _Answers; }
        }

        public ResourceRecordCollection Authorities
        {
            get { return _Authorities; }
        }

        public ResourceRecordCollection AdditionalRecords
        {
            get { return _AdditionalRecords; }
        }

        /// <summary>
        /// Unified collection of Resource Records from Answers, 
        /// Authorities and Additional. NOT IN REALTIME SYNC. 
        /// 
        /// </summary>
        public ResourceRecordCollection ResourceRecords
        {
            get
            {
                if (_ResourceRecords.Count == 0 && _Answers.Count > 0 && _Authorities.Count > 0 && _AdditionalRecords.Count > 0)
                {
                    foreach (ResourceRecord rr in Answers)
                        this._ResourceRecords.Add(rr);

                    foreach (ResourceRecord rr in Authorities)
                        this._ResourceRecords.Add(rr);

                    foreach (ResourceRecord rr in AdditionalRecords)
                        this._ResourceRecords.Add(rr);
                }

                return _ResourceRecords;
            }
        }

        public DnsResponse(int ID, bool AA, bool TC, bool RD, bool RA, int RC)
        {
            this._QueryID = ID;
            this._AuthorativeAnswer = AA;
            this._IsTruncated = TC;
            this._RecursionDesired = RD;
            this._RecursionAvailable = RA;
            this._ResponseCode = (ResponseCodes)RC;

            this._ResourceRecords = new ResourceRecordCollection();
            this._Answers = new ResourceRecordCollection();
            this._Authorities = new ResourceRecordCollection();
            this._AdditionalRecords = new ResourceRecordCollection();
        }
    }
}
