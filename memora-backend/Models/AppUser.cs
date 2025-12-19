using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}