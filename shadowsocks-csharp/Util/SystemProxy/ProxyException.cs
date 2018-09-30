using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
