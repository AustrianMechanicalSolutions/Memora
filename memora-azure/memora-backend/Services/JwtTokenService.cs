using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SigningKey { get; set; } = "";
    public int ExpMinutes { get; set; } = 60;
}

public interface IJwtTokenService
{
    string CreateToken(AppUser user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opts;

    public JwtTokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

    public string CreateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.ExpMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}