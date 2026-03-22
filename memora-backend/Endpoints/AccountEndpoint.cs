using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Endpoints;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountEndpoint : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    public AccountEndpoint(AppDbContext db, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var uid = User.UserId();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (user is null) throw new ApiException("not_found", "User not found.", 404);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            user.Bio,
            user.Status,
            user.BirthDate,
            user.ProfileImageUrl,
            user.PhoneNumber,
            user.DiscordTag,
            user.InstagramUrl,
            user.TikTokUrl,
            user.YouTubeUrl,
            user.WebsiteUrl,
            user.TwoFactorEnabled,
        });
    }

    public class UpdateProfileRequest
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? ProfileImageUrl { get; set; }

        public string? PhoneNumber { get; set; }
        public string? DiscordTag { get; set; }

        public string? InstagramUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? YouTubeUrl { get; set; }
        public string? WebsiteUrl { get; set; }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var uid = User.UserId();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (user is null) throw new ApiException("not_found", "User not found.", 404);

        if (req.DisplayName is not null) user.DisplayName = req.DisplayName.Trim();
        user.Bio = req.Bio;
        user.Status = req.Status;
        user.BirthDate = req.BirthDate;
        user.ProfileImageUrl = req.ProfileImageUrl;

        user.PhoneNumber = req.PhoneNumber;
        user.DiscordTag = req.DiscordTag;

        user.InstagramUrl = req.InstagramUrl;
        user.TikTokUrl = req.TikTokUrl;
        user.YouTubeUrl = req.YouTubeUrl;
        user.WebsiteUrl = req.WebsiteUrl;

        await _db.SaveChangesAsync();
        return Ok();
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var uid = User.UserId();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (user is null) throw new ApiException("not_found", "User not found.", 404);

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
            throw new ApiException("not_found", "User not found.", 404);

        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8)
            throw new ApiException("invalid_password", "Password must be at least 8 characters.");

        if (req.NewPassword == req.CurrentPassword)
            throw new ApiException("invalid_password", "New password must be different.");

        await _db.SaveChangesAsync();

        return Ok();
    }
}
