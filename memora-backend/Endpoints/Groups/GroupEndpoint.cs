using AuthApi.Data;
using AuthApi.Dtos;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public record ChangeRoleRequest(string Role);

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : BaseApiController
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
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ApiException("invalid_name", "Group name is required.");

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

        return CreatedAtAction(nameof(Detail), new { groupId = g.Id },
            new GroupDetailDto(g.Id, g.Name, g.InviteCode, g.Members.Count, g.CreatedByUserId));
    }

    [HttpPost("join")]
    public async Task<ActionResult<GroupDetailDto>> Join([FromBody] JoinGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.InviteCode)) 
            throw new ApiException("invalid_code", "Invite code is required");

        var uid = User.UserId();
        var g = await _db.Set<Group>()
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.InviteCode == req.InviteCode);

        if (g is null) throw new ApiException("invalid_code", "Invalid invite code.", 404);

        var exists = g.Members.Any(x => x.UserId == uid);
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

        var g = await _db.Set<Group>()
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == groupId);

        if (g is null)
            throw new ApiException("not_found", "Group not found.", 404);

        if (!g.Members.Any(x => x.UserId == uid))
            throw new ApiException("forbidden", "You are not a member of this group.", 403);

        var owner = await _db.Set<AppUser>()
            .Where(u => u.Id == g.CreatedByUserId)
            .Select(u => u.DisplayName)
            .FirstAsync();

        return Ok(new GroupDetailInfoDto(g.Id, g.Name, g.InviteCode, g.Members.Count, owner ?? "Unknown"));
    }

    [HttpPatch("{groupId:guid}")]
    public async Task<IActionResult> Rename(Guid groupId, RenameGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ApiException("invalid_name", "Name cannot be empty.");

        var uid = User.UserId();

        var member = await _db.Set<GroupMember>()
            .Include(x => x.Group)
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == uid);

        if (member is null)
            throw new ApiException("not_found", "Group not found.", 404);

        if (member.Role != GroupRole.Admin)
            throw new ApiException("forbidden", "You don't have the permissions to change this group's name.", 403);

        member.Group.Name = req.Name.Trim();

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

        if (group is null)
            throw new ApiException("not_found", "Group not found.", 404);

        if (group.CreatedByUserId != uid)
            throw new ApiException("not_allowed", "Only the owner can delete the group.", 403);

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
            throw new ApiException("not_found", "Group not found.", 404);

        if (group.CreatedByUserId != uid)
            throw new ApiException("forbidden", "You don't have the permissions to regenerate this group's invite code.", 403);

        group.InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await _db.SaveChangesAsync();

        return Ok(new { inviteCode = group.InviteCode });
    }
}
