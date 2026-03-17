namespace AuthApi.Models;

public class MemoryLike
{
    public Guid MemoryId { get; set; }
    public Memory Memory { get; set; } = default!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MemoryComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemoryId { get; set; }
    public Memory Memory { get; set; } = default!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ParentCommentId { get; set; }
    public MemoryComment? ParentComment { get; set; }
}

public class CommentLike
{
    public Guid CommentId { get; set; }
    public MemoryComment Comment { get; set; } = default!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
