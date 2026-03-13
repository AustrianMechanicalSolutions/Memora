public class Album
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }

    public string Title { get; set; } = "";
    public string? Description { get; set; }

    public DateTime DateStart { get; set; }
    public DateTime? DateEnd { get; set; }

    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }

    public Group Group { get; set; } = null!;
    public List<Memory> Memories { get; set; } = new();

    public ICollection<AlbumPerson> People { get; set; } = new List<AlbumPerson>();
}