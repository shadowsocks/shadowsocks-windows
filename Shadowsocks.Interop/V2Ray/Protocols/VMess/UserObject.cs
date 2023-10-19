using System;

namespace Shadowsocks.Interop.V2Ray.Protocols.VMess;

/// <summary>
/// The user object for VMess AEAD.
/// </summary>
public class UserObject(string id = "")
{
    public string Id { get; set; } = id;
    public string? Email { get; set; }
    public int Level { get; set; }

    public static UserObject Default => new()
    {
        Id = Guid.NewGuid().ToString(),
    };
}