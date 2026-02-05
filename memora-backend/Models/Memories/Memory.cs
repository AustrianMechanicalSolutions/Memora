public class Memory
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = default!;

    public MemoryType Type { get; set; }
    public string? Title { get; set; }
    public string? QuoteText { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbUrl { get; set; }
<<<<<<< HEAD
    public string? QuoteBy { get; set; }
=======

>>>>>>> origin/main
    public DateTime HappenedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; } = default!;

    public ICollection<MemoryTag> Tags { get; set; } = new List<MemoryTag>();
}

public class MemoryTag
{
    public Guid MemoryId { get; set; }
    public Memory Memory { get; set; } = default!;
    public string Value { get; set; } = default!;
}