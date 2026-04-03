public class MemoryPerson
{
    public Guid Id { get; set; }
    public Guid MemoryId { get; set; }
    public Memory Memory { get; set; } = default!;
    public string Name { get; set; } = default!;
}