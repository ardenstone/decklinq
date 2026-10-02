using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.Models;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public class AuthService
{
    private readonly List<User> _users = new();
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _securityKey;

    public AuthService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? "decklinq";
        _audience = configuration["Jwt:Audience"] ?? "decklinq-users";
        var key = configuration["Jwt:Key"] ?? "dev-local-secret-key-for-decklinq-mvp";
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        SeedDemoUser();
    }

    public User? Register(string username, string password)
    {
        if (_users.Any(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var user = new User
        {
            Id = _users.Count == 0 ? 1 : _users.Max(existing => existing.Id) + 1,
            Username = username.Trim(),
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _users.Add(user);
        return user;
    }

    public User? Authenticate(string username, string password)
    {
        var user = _users.FirstOrDefault(existing =>
            string.Equals(existing.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));

        if (user is null || !VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public User? GetCurrentUser(ClaimsPrincipal principal)
    {
        var userIdValue = principal.GetUserIdValue();
        if (string.IsNullOrWhiteSpace(userIdValue) || !int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return _users.FirstOrDefault(user => user.Id == userId);
    }

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SeedDemoUser()
    {
        if (_users.Count != 0)
        {
            return;
        }

        Register("demo", "password123");
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var separator = storedHash.IndexOf(':');
        if (separator < 0)
        {
            return false;
        }

        var salt = Convert.FromBase64String(storedHash[..separator]);
        var hash = Convert.FromBase64String(storedHash[(separator + 1)..]);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        return CryptographicOperations.FixedTimeEquals(computedHash, hash);
    }
}
