namespace Memora.Api.Models;

public class MemoryTag
{
    public int MemoryId { get; set; }
    public Memory Memory { get; set; } = default!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}