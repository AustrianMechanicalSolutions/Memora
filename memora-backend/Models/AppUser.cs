using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
<<<<<<< HEAD
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
=======
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
>>>>>>> origin/main

    // Profile Fields
    [MaxLength(50)]
    public string DisplayName { get; set; } = "New User";

    public string? Bio { get; set; }
    public string? Status { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? ProfileImageUrl { get; set; }

    // Contact
    public string? PhoneNumber { get; set; }

    // Socials
    public string? DiscordTag { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TikTokUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    // TOTP
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; } // base32 secret
<<<<<<< HEAD
    public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
=======
>>>>>>> origin/main
}