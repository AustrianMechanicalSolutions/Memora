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
public class TwoFactorController : ControllerBase
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
        if (user is null) return NotFound();

        // Generate new secret every time setup is called
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(secretBytes);

        user.TwoFactorSecret = secretBase32;
        user.TwoFactorEnabled = true;
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
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
            return BadRequest("2FA secret not set. Run setup first.");

        var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
        var ok = totp.VerifyTotp(req.Code.Trim(), out _, new VerificationWindow(1, 1));

        if (!ok) return BadRequest("Invalid 2FA code.");

        user.TwoFactorEnabled = true;
        await _db.SaveChangesAsync();

        return Ok(new { enabled = true });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable()
    {
        var uid = User.UserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (user is null) return NotFound();

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _db.SaveChangesAsync();

        return Ok(new { enabled = false });
    }
}