using System;

namespace Shadowsocks.Interop.V2Ray.Protocols.VMess
{
    /// <summary>
    /// The user object for VMess AEAD.
    /// </summary>
    public class UserObject
    {
        public string Id { get; set; }
        public string? Email { get; set; }
        public int Level { get; set; }

        public UserObject(string id = "")
        {
            Id = id;
        }

        public static UserObject Default => new()
        {
            Id = Guid.NewGuid().ToString(),
        };
    }
}
