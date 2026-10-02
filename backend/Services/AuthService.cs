using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _securityKey;

    public AuthService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _issuer = configuration["Jwt:Issuer"] ?? "decklinq";
        _audience = configuration["Jwt:Audience"] ?? "decklinq-users";
        var key = configuration["Jwt:Key"] ?? "dev-local-secret-key-for-decklinq-mvp";
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        SeedDemoUser();
    }

    public User? Register(string username, string password)
    {
        var normalizedUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return null;
        }

        if (_db.Users.Any(user => user.Username.ToLower() == normalizedUsername.ToLower()))
        {
            return null;
        }

        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    public User? Authenticate(string username, string password)
    {
        var normalizedUsername = username.Trim();
        var user = _db.Users.FirstOrDefault(existing =>
            existing.Username.ToLower() == normalizedUsername.ToLower());

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

        return _db.Users.Find(userId);
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
        if (_db.Users.Any())
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
