using System;
using System.Runtime.Serialization;

namespace Shadowsocks.Util.SystemProxy
{
    enum ProxyExceptionType
    {
        Unspecific,
        FailToRun,
        QueryReturnEmpty,
        SysproxyExitError,
        QueryReturnMalformed
    }

    class ProxyException : Exception
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
            Type = type;
        }

        public ProxyException(ProxyExceptionType type, string message) : base(message)
        {
            Type = type;
        }

        public ProxyException(ProxyExceptionType type, string message, Exception innerException) : base(message, innerException)
        {
            Type = type;
        }

        protected ProxyException(ProxyExceptionType type, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Type = type;
        }
    }
}
