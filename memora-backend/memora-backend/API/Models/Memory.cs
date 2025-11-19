using Memora.Api.Models;

public class Memory
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Text { get; set; }

    // z.B. URL zu Bild / Video (Cloud Storage, CDN etc...)
    public string? MediaUrl { get; set; }

    public MemoryType Type { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<MemoryTag> MemoryTags { get; set; } = new List<MemoryTag>();
    public ICollection<MemoryLike> Likes { get; set; } = new List<MemoryLike>();
}