using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public record ChangeRoleRequest(string Role);

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public GroupsController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<List<GroupListItemDto>>> MyGroups()
    {
        var uid = User.UserId();
        var groups = await _db.Set<GroupMember>()
            .Where(m => m.UserId == uid)
            .Select(m => m.Group)
            .Select(g => new GroupListItemDto(g.Id, g.Name, g.Members.Count))
            .ToListAsync();

        return Ok(groups);
    }

    [HttpPost]
    public async Task<ActionResult<GroupDetailDto>> Create([FromBody] CreateGroupRequest req)
    {
        var uid = User.UserId();
        var g = new Group
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CreatedByUserId = uid
        };
        g.Members.Add(new GroupMember { GroupId = g.Id, UserId = uid, Role = GroupRole.Admin });

        _db.Add(g);
        await _db.SaveChangesAsync();

        return Ok(new GroupDetailDto(g.Id, g.Name, g.InviteCode, g.Members.Count, g.CreatedByUserId));
    }

    [HttpPost("join")]
    public async Task<ActionResult<GroupDetailDto>> Join([FromBody] JoinGroupRequest req)
    {
        var uid = User.UserId();
        var g = await _db.Set<Group>().Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.InviteCode == req.InviteCode);

        if (g is null) return NotFound("Invite code not found.");

        var exists = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == g.Id && x.UserId == uid);
        if (!exists)
        {
            _db.Add(new GroupMember { GroupId = g.Id, UserId = uid, Role = GroupRole.Member });
            await _db.SaveChangesAsync();
        }

        return Ok(new GroupDetailDto(g.Id, g.Name, g.InviteCode, g.Members.Count, g.CreatedByUserId));
    }

    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<GroupDetailDto>> Detail(Guid groupId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var g = await _db.Set<Group>().Include(x => x.Members).FirstAsync(x => x.Id == groupId);

        var owner = await _db.Set<AppUser>()
            .Where(u => u.Id == g.CreatedByUserId)
            .Select(u => u.DisplayName)
            .FirstAsync();

        return Ok(new GroupDetailInfoDto(g.Id, g.Name, g.InviteCode, g.Members.Count, owner));
    }

    [HttpPatch("{groupId:guid}")]
    public async Task<IActionResult> Rename(Guid groupId, RenameGroupRequest req)
    {
        var uid = User.UserId();

        var group = await _db.Set<Group>()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return NotFound();

        // optional: check permission
        if (group.CreatedByUserId != uid)
            return Forbid();

        group.Name = req.Name;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> Delete(Guid groupId)
    {
        var uid = User.UserId();

        var group = await _db.Set<Group>()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return NotFound();

        if (group.CreatedByUserId != uid)
            return StatusCode(403, "Only the owner can delete the group.");

        _db.Remove(group);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{groupId:guid}/invite/regenerate")]
    public async Task<IActionResult> RegenerateInvite(Guid groupId)
    {
        var uid = User.UserId();

        var group = await _db.Set<Group>()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return NotFound();

        if (group.CreatedByUserId != uid)
            return Forbid();

        group.InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await _db.SaveChangesAsync();

        return Ok(new { inviteCode = group.InviteCode });
    }
}
