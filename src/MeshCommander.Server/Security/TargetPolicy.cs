using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace MeshCommander.Server.Security;

public sealed class TargetPolicy
{
    private readonly string[] entries;

    public TargetPolicy()
    {
        entries = (Environment.GetEnvironmentVariable("MCE_ALLOWED_TARGETS") ?? "private")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task<IPEndPoint> ResolveAllowedEndpointAsync(string host, int port, CancellationToken cancellationToken)
    {
        if (port is < 1 or > 65535)
        {
            throw new InvalidOperationException("Invalid target port.");
        }

        if (string.IsNullOrWhiteSpace(host) || host.Length > 253)
        {
            throw new InvalidOperationException("Invalid target host.");
        }

        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        var allowed = addresses
            .Where(address => address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
            .Where(address => IsAllowed(host, address))
            .ToArray();

        if (allowed.Length == 0)
        {
            throw new InvalidOperationException($"Target is blocked by MCE_ALLOWED_TARGETS: {host}");
        }

        return new IPEndPoint(allowed[0], port);
    }

    public bool IsAllowed(string host, IPAddress address)
    {
        if (IsAlwaysBlocked(address))
        {
            return false;
        }

        foreach (var entry in entries)
        {
            if (entry == "*")
            {
                return true;
            }

            if (entry.Equals("private", StringComparison.OrdinalIgnoreCase) && IsPrivate(address))
            {
                return true;
            }

            if (entry.StartsWith("*.", StringComparison.Ordinal) &&
                host.EndsWith(entry[1..], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (entry.Contains('/', StringComparison.Ordinal) && CidrContains(entry, address))
            {
                return true;
            }

            if (IPAddress.TryParse(entry, out var allowedAddress) && allowedAddress.Equals(address))
            {
                return true;
            }

            if (entry.Equals(host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAlwaysBlocked(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.Broadcast) || address.IsIPv6Multicast)
        {
            return true;
        }

        var bytes = address.MapToIPv4().GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xfe) == 0xfc;
        }

        var b = address.GetAddressBytes();
        return b[0] == 10 ||
               (b[0] == 172 && b[1] is >= 16 and <= 31) ||
               (b[0] == 192 && b[1] == 168);
    }

    private static bool CidrContains(string cidr, IPAddress address)
    {
        var parts = cidr.Split('/', 2);
        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var network) ||
            !int.TryParse(parts[1], out var prefix))
        {
            return false;
        }

        var networkBytes = network.MapToIPv4().GetAddressBytes();
        var addressBytes = address.MapToIPv4().GetAddressBytes();
        if (prefix is < 0 or > 32)
        {
            return false;
        }

        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        var networkValue = BinaryPrimitives.ReadUInt32BigEndian(networkBytes);
        var addressValue = BinaryPrimitives.ReadUInt32BigEndian(addressBytes);
        return (networkValue & mask) == (addressValue & mask);
    }
}
