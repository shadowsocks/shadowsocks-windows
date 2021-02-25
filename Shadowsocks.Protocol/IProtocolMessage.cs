using System;

namespace Shadowsocks.Protocol
{
    public interface IProtocolMessage : IEquatable<IProtocolMessage>
    {
        public int Serialize(Memory<byte> buffer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>Tuple represent load state,
        /// when success, length is how many byte has been taken
        /// when fail, length is how many byte required, 0 is parse error
        /// </returns>
        public (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer);
    }
}