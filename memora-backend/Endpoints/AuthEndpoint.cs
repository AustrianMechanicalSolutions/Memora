using AuthApi.Dtos;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace AuthApi.Endpoints;

[ApiController]
[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly IJwtTokenService _jwtSvc;

    public AuthController(
        AppDbContext db,
        IPasswordHasher<AppUser> hasher,
        IJwtTokenService jwtSvc)
    {
        _db = db;
        _hasher = hasher;
        _jwtSvc = jwtSvc;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains("@"))
            throw new ApiException("bad_request", "Invalid email", 400);

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new ApiException("conflict", "Email already exists", 409);

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
            throw new ApiException("bad_request", "Password must be at least 8 characters", 400);

        var user = new AppUser { Email = email };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(_jwtSvc.CreateToken(user)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // prevent time-attacks by always validating a password or email, even if the user doesn't exist
        var fakeUser = new AppUser();
        var hash = user?.PasswordHash ?? _hasher.HashPassword(fakeUser, "fake");

        var result = _hasher.VerifyHashedPassword(user ?? fakeUser, hash, req.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
            throw new ApiException("unauthorized", "Invalid login credentials", 401);

        if (result == PasswordVerificationResult.Failed)
            throw new ApiException("unauthorized", "Invalid login credentials", 401);

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                throw new ApiException("unauthorized", "2fa not configured", 401);

            if (string.IsNullOrWhiteSpace(req.TwoFactorCode))
                throw new ApiException("unauthorized", "2fa is required", 401);

            var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
            var ok = totp.VerifyTotp(req.TwoFactorCode.Trim(), out _, new VerificationWindow(1, 1));

            if (!ok)
                throw new ApiException("unauthorized", "2fa code is invalid", 401);
        }

        return Ok(new AuthResponse(_jwtSvc.CreateToken(user)));
    }
}