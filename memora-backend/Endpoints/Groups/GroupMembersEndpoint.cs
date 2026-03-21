using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/members")]
[Authorize]
public class GroupMembersController : ControllerBase
{
    private readonly AppDbContext _db;

    public GroupMembersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet()]
    public async Task<ActionResult<List<GroupMemberDto>>> Members(Guid groupId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var members = await _db.Set<GroupMember>()
            .AsNoTracking()
            .Where(x => x.GroupId == groupId)
            .Join(
                _db.Set<AppUser>(),
                gm => gm.UserId,
                u => u.Id,
                (gm, u) => new { gm, u }
            )
            .OrderBy(x => x.u.DisplayName)
            .Select(x => new GroupMemberDto(
                x.gm.UserId,
                x.u.DisplayName,
                x.gm.Role.ToString(),
                x.u.ProfileImageUrl
            ))
            .ToListAsync();

        return Ok(members);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId)
    {
        var uid = User.UserId();

        var me = await _db.Set<GroupMember>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == uid);

        if (me == null) return Forbid();
        if (me.Role != GroupRole.Admin) return Forbid();

        var target = await _db.Set<GroupMember>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (target == null) return NotFound();

        // prevent removing yourself (optional safety)
        if (target.UserId == uid)
            return StatusCode(403, "You cannot remove yourself.");

        _db.Remove(target);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid groupId, Guid userId, [FromBody] ChangeRoleRequest req)
    {
        var uid = User.UserId();

        var me = await _db.Set<GroupMember>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == uid);

        if (me == null || me.Role != GroupRole.Admin)
            return Forbid();

        var target = await _db.Set<GroupMember>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (target == null) return NotFound();

        if (!Enum.TryParse<GroupRole>(req.Role, true, out var newRole))
            return BadRequest("Invalid role.");

        if (target.Role == GroupRole.Admin && newRole != GroupRole.Admin)
        {
            var adminCount = await _db.Set<GroupMember>()
                .CountAsync(x => x.GroupId == groupId && x.Role == GroupRole.Admin);

            if (adminCount <= 1)
                return StatusCode(403, "Group must have at least one admin.");
        }

        target.Role = newRole;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}