using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Post34.DTOs;
using Post34.Helpers;
using Post34.Models;
using Post34.Repositories;

namespace Post34.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly JwtSettings _jwt;

    public AuthService(IUserRepository repo, IOptions<JwtSettings> jwtOptions)
    {
        _repo = repo;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _repo.GetByUsernameAsync(request.Username);
        if (user == null) return null;

        if (!VerifyPassword(request.Password, user.passwordHash)) return null;

        var jwt = GenerateToken(user);
        return new AuthResponse { Token = jwt.token, ExpiresAt = jwt.expiresAt };
    }

    private (string token, DateTime expiresAt) GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.username),
            new Claim(ClaimTypes.Name, user.username)
        };

        if (!string.IsNullOrEmpty(user.role))
            claims.Add(new Claim(ClaimTypes.Role, user.role));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    // PBKDF2 password hashing helpers
    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 3);
        if (parts.Length != 3) return false;
        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var hash = Convert.FromBase64String(parts[2]);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var computed = pbkdf2.GetBytes(hash.Length);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }

    public static string HashPassword(string password, int iterations = 100_000)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return string.Join('.', iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }
}
