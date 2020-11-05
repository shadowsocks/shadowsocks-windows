using System;
using System.Runtime.Serialization;

namespace Shadowsocks.Net.SystemProxy
{
    public enum ProxyExceptionType
    {
        Unspecific,
        FailToRun,
        QueryReturnEmpty,
        SysproxyExitError,
        QueryReturnMalformed
    }

    public class ProxyException : Exception
    {
        // provide more specific information about exception
        public ProxyExceptionType Type { get; }

        public ProxyException()
        {
        }

        public ProxyException(string message) : base(message)
        {
        }

        public ProxyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ProxyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        public ProxyException(ProxyExceptionType type)
        {
            this.Type = type;
        }

        public ProxyException(ProxyExceptionType type, string message) : base(message)
        {
            this.Type = type;
        }

        public ProxyException(ProxyExceptionType type, string message, Exception innerException) : base(message, innerException)
        {
            this.Type = type;
        }

        protected ProxyException(ProxyExceptionType type, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.Type = type;
        }
    }
}
