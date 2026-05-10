using AuthApi.Data;
using AuthApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace AuthApi.Endpoints;

[ApiController]
[Route("api/2fa")]
[Authorize]
public class TwoFactorController : BaseApiController
{
    private readonly AppDbContext _db;

    public TwoFactorController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup()
    {
        var uid = User.UserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (user is null) throw new ApiException("not_found", "User not found.", 404);

        // Generate new secret every time setup is called
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(secretBytes);

        user.TwoFactorSecret = secretBase32;
        user.TwoFactorEnabled = false;
        await _db.SaveChangesAsync();

        var issuer = "Memora";
        var label = $"{issuer}:{user.Email}";
        var otpauth = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secretBase32}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

        return Ok(new
        {
            secret = secretBase32,
            otpauthUrl = otpauth
        });
    }

    public class Enable2FARequest
    {
        public string Code { get; set; } = "";
    }

    [HttpPost("enable")]
    public async Task<IActionResult> Enable([FromBody] Enable2FARequest req)
    {
        var uid = User.UserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (user is null) throw new ApiException("not_found", "User not found.", 404);

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
            throw new ApiException("invalid_code", "2FA secret not set.");

        var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
        var ok = totp.VerifyTotp(req.Code.Trim(), out _, new VerificationWindow(1, 1));

        if (!ok) throw new ApiException("invalid_code", "Invalid 2FA code.");

        user.TwoFactorEnabled = true;
        await _db.SaveChangesAsync();

        return Ok(new { enabled = true });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable()
    {
        var uid = User.UserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (user is null) throw new ApiException("not_found", "User not found.", 404);

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _db.SaveChangesAsync();

        return Ok(new { enabled = false });
    }
}