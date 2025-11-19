namespace Memora.Api.Models;

public class MemoryLike
{
    public int MemoryId { get; set; }
    public Memory Memory { get; set; } = default!;

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}