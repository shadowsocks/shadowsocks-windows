using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Shadowsocks._3rd.ProxySocket
{
    public class SocksHttpWebRequest : WebRequest
    {

        #region Private
        private Encoding _correctEncoding;
        #endregion Private

        #region Member Variables

        private readonly Uri _requestUri;
        private WebHeaderCollection _requestHeaders;
        private string _method;
        private SocksHttpWebResponse _response;
        private string _requestMessage;
        private byte[] _requestContentBuffer;

        // darn MS for making everything internal (yeah, I'm talking about you, System.net.KnownHttpVerb)
        static readonly StringCollection validHttpVerbs =
            new StringCollection { "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "OPTIONS" };

        #endregion

        #region Constructor

        private SocksHttpWebRequest(Uri requestUri)
        {
            _requestUri = requestUri;
            _correctEncoding = Encoding.Default;
        }

        #endregion

        #region WebRequest Members

        public override WebResponse GetResponse()
        {
            if (Proxy == null)
            {
                throw new InvalidOperationException("Proxy property cannot be null.");
            }
            if (String.IsNullOrEmpty(Method))
            {
                throw new InvalidOperationException("Method has not been set.");
            }

            if (RequestSubmitted)
            {
                return _response;
            }
            _response = InternalGetResponse();
            RequestSubmitted = true;
            return _response;
        }

        public override Uri RequestUri
        {
            get { return _requestUri; }
        }

        public override IWebProxy Proxy { get; set; }

        public override WebHeaderCollection Headers
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = new WebHeaderCollection();
                }
                return _requestHeaders;
            }
            set
            {
                if (RequestSubmitted)
                {
                    throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
                }
                _requestHeaders = value;
            }
        }

        public bool RequestSubmitted { get; private set; }

        public override string Method
        {
            get
            {
                return _method ?? "GET";
            }
            set
            {
                if (validHttpVerbs.Contains(value))
                {
                    _method = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", string.Format("'{0}' is not a known HTTP verb.", value));
                }
            }
        }

        public override long ContentLength { get; set; }

        public override string ContentType { get; set; }

        public override Stream GetRequestStream()
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }

            if (_requestContentBuffer == null)
            {
                _requestContentBuffer = new byte[ContentLength];
            }
            else if (ContentLength == default(long))
            {
                _requestContentBuffer = new byte[int.MaxValue];
            }
            else if (_requestContentBuffer.Length != ContentLength)
            {
                Array.Resize(ref _requestContentBuffer, (int)ContentLength);
            }
            return new MemoryStream(_requestContentBuffer);
        }

        #endregion

        #region Methods

        public static new WebRequest Create(string requestUri)
        {
            return new SocksHttpWebRequest(new Uri(requestUri));
        }

        public static new WebRequest Create(Uri requestUri)
        {
            return new SocksHttpWebRequest(requestUri);
        }

        private string BuildHttpRequestMessage()
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
            }

            var message = new StringBuilder();
            message.AppendFormat("{0} {1} HTTP/1.0\r\nHost: {2}\r\n", Method, RequestUri.PathAndQuery, RequestUri.Host);

            // add the headers
            foreach (var key in Headers.Keys)
            {
                message.AppendFormat("{0}: {1}\r\n", key, Headers[key.ToString()]);
            }

            if (!string.IsNullOrEmpty(ContentType))
            {
                message.AppendFormat("Content-Type: {0}\r\n", ContentType);
            }
            if (ContentLength > 0)
            {
                message.AppendFormat("Content-Length: {0}\r\n", ContentLength);
            }

            // add a blank line to indicate the end of the headers
            message.Append("\r\n");

            // add content
            if (_requestContentBuffer != null && _requestContentBuffer.Length > 0)
            {
                using (var stream = new MemoryStream(_requestContentBuffer, false))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        message.Append(reader.ReadToEnd());
                    }
                }
            }

            return message.ToString();
        }

        private SocksHttpWebResponse InternalGetResponse()
        {
            var response = new StringBuilder();
            using (var _socksConnection =
                new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var proxyUri = Proxy.GetProxy(RequestUri);
                var ipAddress = GetProxyIpAddress(proxyUri);
                _socksConnection.ProxyEndPoint = new IPEndPoint(ipAddress, proxyUri.Port);
                _socksConnection.ProxyType = ProxyTypes.Socks5;

                

                // open connection
                _socksConnection.Connect(RequestUri.Host, 80);
                // send an HTTP request
                _socksConnection.Send(_correctEncoding.GetBytes(RequestMessage));
                // read the HTTP reply
                var buffer = new byte[1024];

                var bytesReceived = _socksConnection.Receive(buffer);
                while (bytesReceived > 0)
                {
                    string chunk = _correctEncoding.GetString(buffer, 0, bytesReceived);
                    string encString = EncodingHelper.GetEncodingFromChunk(chunk);
                    if (!string.IsNullOrEmpty(encString))
                    {
                        try
                        {
                            _correctEncoding = Encoding.GetEncoding(encString);
                        }
                        catch
                        {
                            //TODO: do something here
                        }
                    }
                    response.Append(chunk);
                    bytesReceived = _socksConnection.Receive(buffer);
                }
            }
            return new SocksHttpWebResponse(response.ToString(),_correctEncoding);
        }

        private static IPAddress GetProxyIpAddress(Uri proxyUri)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(proxyUri.Host, out ipAddress))
            {
                try
                {
                    return Dns.GetHostEntry(proxyUri.Host).AddressList[0];
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        string.Format("Unable to resolve proxy hostname '{0}' to a valid IP address.", proxyUri.Host), e);
                }
            }
            return ipAddress;
        }

        #endregion

        #region Properties

        public Encoding CorrectEncoding
        {
            get
            {
                return _correctEncoding;
            }
        }

        public string RequestMessage
        {
            get
            {
                if (string.IsNullOrEmpty(_requestMessage))
                {
                    _requestMessage = BuildHttpRequestMessage();
                }
                return _requestMessage;
            }
        }

        #endregion

    }
}
