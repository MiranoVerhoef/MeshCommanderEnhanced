using System.Net;
using MeshCommander.Server.Security;
using Xunit;

namespace MeshCommander.Server.Tests;

public sealed class TargetPolicyTests
{
    [Fact]
    public void DefaultPolicyAllowsPrivateAndLoopbackTargets()
    {
        WithAllowedTargets(null, () =>
        {
            var policy = new TargetPolicy();

            Assert.True(policy.IsAllowed("192.168.1.20", IPAddress.Parse("192.168.1.20")));
            Assert.True(policy.IsAllowed("127.0.0.1", IPAddress.Parse("127.0.0.1")));
        });
    }

    [Fact]
    public void DefaultPolicyBlocksPublicAndLinkLocalTargets()
    {
        WithAllowedTargets(null, () =>
        {
            var policy = new TargetPolicy();

            Assert.False(policy.IsAllowed("example.com", IPAddress.Parse("93.184.216.34")));
            Assert.False(policy.IsAllowed("169.254.169.254", IPAddress.Parse("169.254.169.254")));
        });
    }

    [Fact]
    public void CidrEntryAllowsMatchingTargets()
    {
        WithAllowedTargets("10.42.0.0/16", () =>
        {
            var policy = new TargetPolicy();

            Assert.True(policy.IsAllowed("10.42.10.5", IPAddress.Parse("10.42.10.5")));
            Assert.False(policy.IsAllowed("10.43.10.5", IPAddress.Parse("10.43.10.5")));
        });
    }

    private static void WithAllowedTargets(string? value, Action action)
    {
        var previous = Environment.GetEnvironmentVariable("MCE_ALLOWED_TARGETS");
        try
        {
            Environment.SetEnvironmentVariable("MCE_ALLOWED_TARGETS", value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCE_ALLOWED_TARGETS", previous);
        }
    }
}
