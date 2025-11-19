namespace Memora.Api.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public ICollection<MemoryTag> MemoryTags { get; set; } = new List<MemoryTag>();
}