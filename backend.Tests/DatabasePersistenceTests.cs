using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace backend.Tests;

public class DatabasePersistenceTests
{
    [Fact]
    public void Register_StoresUserInDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Key"] = "super-secret-test-key-1234567890"
            })
            .Build();

        var service = new AuthService(context, config);

        var user = service.Register("db-user", "password123");

        Assert.NotNull(user);
        Assert.Contains(context.Users, entry => entry.Username == "db-user" && entry.Id == user!.Id);
    }
}
