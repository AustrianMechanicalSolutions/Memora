using System.ComponentModel.DataAnnotations;

namespace AuthApi.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
<<<<<<< HEAD
    [Required] string Password,
    string? TwoFactorCode
=======
    [Required] string Password
>>>>>>> origin/main
);

public record AuthResponse(string Token);