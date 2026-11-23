using System.Security.Cryptography;
using System.Text;

namespace WasteSubmitStubs.Datasets;

/// <summary>
/// Name-based UUIDs (version 5, RFC 9562): the same name always gives the same ID. Generated datasets use them so
/// every replica, every restart and the perf suite agree on the IDs, without storing any state.
/// </summary>
public static class DeterministicGuid
{
    /// <summary>The namespace for every ID the stubs generate.</summary>
    public static readonly Guid StubsNamespace = Guid.Parse("6f1c2a52-4f0e-4a3b-9a4e-2b7d8f1e5c30");

    public static Guid Create(string name) => Create(StubsNamespace, name);

    public static Guid Create(Guid namespaceId, string name)
    {
        Span<byte> namespaceBytes = stackalloc byte[16];
        namespaceId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        var input = new byte[16 + nameBytes.Length];
        namespaceBytes.CopyTo(input);
        nameBytes.CopyTo(input, 16);

        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(input, hash);

        var bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50); // version 5
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 9562 variant
        return new Guid(bytes, bigEndian: true);
    }
}
