using System.Security.Claims;

namespace Grit.BuildingBlocks.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_SubClaimWithAGuid_ReturnsIt()
    {
        var id = Guid.CreateVersion7();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Bearer"));

        user.GetUserId().ShouldBe(id);
    }

    [Fact]
    public void GetUserId_Anonymous_ReturnsNull()
    {
        new ClaimsPrincipal(new ClaimsIdentity()).GetUserId().ShouldBeNull();
    }

    [Fact]
    public void GetUserId_SubClaimNotAGuid_ReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "ana@grit.dev")], "Bearer"));

        user.GetUserId().ShouldBeNull();
    }
}
