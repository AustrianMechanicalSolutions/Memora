namespace Memora.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = default!;
    public string DisplayName { get; set; } = default!;

    public ICollection<Memory> Memories { get; set; } = new List<Memory>();
    public ICollection<MemoryLike> Likes { get; set; } = new List<MemoryLike>();
}