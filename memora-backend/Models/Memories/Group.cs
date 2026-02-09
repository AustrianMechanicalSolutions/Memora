public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string InviteCode{ get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<Memory> Memories { get; set; } = new List<Memory>();
}