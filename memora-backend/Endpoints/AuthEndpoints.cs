using AuthApi.Dtos;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace AuthApi.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            RegisterRequest req,
            AppDbContext db,
            IPasswordHasher<AppUser> hasher,
            IJwtTokenService jwtSvc) =>
        {
            var email = req.Email.Trim().ToLowerInvariant();

            if (await db.Users.AnyAsync(u => u.Email == email))
                return Results.Conflict(new { message = "Email already exists" });

            var user = new AppUser { Email = email };
            user.PasswordHash = hasher.HashPassword(user, req.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponse(jwtSvc.CreateToken(user)));
        });

        group.MapPost("/login", async (
            LoginRequest req,
            AppDbContext db,
            IPasswordHasher<AppUser> hasher,
            IJwtTokenService jwtSvc) =>
        {
            var email = req.Email.Trim().ToLowerInvariant();

            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null) return Results.Unauthorized();

            var result = hasher.VerifyHashedPassword(
                user, user.PasswordHash, req.Password);

            if (result == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                    return Results.Json(new { error = "2fa_not_configured" }, statusCode: 401);

                if (string.IsNullOrWhiteSpace(req.TwoFactorCode))
                    return Results.Json(new { error = "2fa_required" }, statusCode: 401);
                    
                var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
                var ok = totp.VerifyTotp(req.TwoFactorCode.Trim(), out _, new VerificationWindow(1, 1));

                if (!ok) return Results.Json(new { error = "2fa_invalid" }, statusCode: 401);
            }

            return Results.Ok(new AuthResponse(jwtSvc.CreateToken(user)));
        });

        return group;
    }
}
