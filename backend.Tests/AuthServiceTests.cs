using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace backend.Tests;

public class AuthServiceTests
{
    private static AuthService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Key"] = "super-secret-test-key-1234567890"
            })
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthService(new AppDbContext(options), config);
    }

    [Fact]
    public void Register_AndAuthenticate_RoundTripsSuccessfully()
    {
        var service = CreateService();

        var user = service.Register("alice", "hunter2");

        Assert.NotNull(user);
        Assert.Equal("alice", user.Username);
        Assert.NotNull(service.Authenticate("alice", "hunter2"));
        Assert.Null(service.Authenticate("alice", "wrong-password"));
    }

    [Fact]
    public void Register_RejectsDuplicateUsername_IgnoringCase()
    {
        var service = CreateService();

        service.Register("charlie", "pass123");

        Assert.Null(service.Register("CHARLIE", "other-pass"));
    }

    [Fact]
    public void CreateToken_ContainsExpectedClaims()
    {
        var service = CreateService();
        var user = service.Register("bob", "pass123");

        var token = service.CreateToken(user!);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(user!.Id.ToString(), jwt.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("bob", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
    }
}
