<<<<<<< HEAD
using AuthApi.Models;

=======
>>>>>>> origin/main
public class GroupMember
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = default!;
    public Guid UserId { get; set; }
    public GroupRole Role { get; set; } = GroupRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
<<<<<<< HEAD
    public AppUser User { get; set; } = null!;
=======
>>>>>>> origin/main
}