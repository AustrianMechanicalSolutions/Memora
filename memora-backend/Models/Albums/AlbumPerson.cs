using AuthApi.Models;

public class AlbumPerson
{
    public Guid AlbumId { get; set; }
    public Album Album { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}