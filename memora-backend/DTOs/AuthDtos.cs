using System.ComponentModel.DataAnnotations;

namespace AuthApi.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    string? TwoFactorCode
);

public record AuthResponse(string Token);