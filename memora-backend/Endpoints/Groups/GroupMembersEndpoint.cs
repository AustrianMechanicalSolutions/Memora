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

    [HttpGet]
    public async Task<ActionResult<List<GroupMemberDto>>> Members(Guid groupId)
    {
        var uid = User.UserId();

        await EnsureGroupMember(groupId, uid);

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

        var me = await GetMember(groupId, uid);

        if (me?.Role != GroupRole.Admin)
            throw new ApiException("forbidden", "Admin access required.", 403);

        var target = await GetMember(groupId, userId);

        if (target == null)
            throw new ApiException("not_found", "Member not found.", 404);

        if (target.UserId == uid)
            throw new ApiException("forbidden", "You cannot remove yourself.", 403);

        _db.Remove(target);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid groupId, Guid userId, [FromBody] ChangeRoleRequest req)
    {
        var uid = User.UserId();

        var me = await GetMember(groupId, uid);

        if (me?.Role != GroupRole.Admin)
            throw new ApiException("forbidden", "Admin access required.", 403);

        var target = await GetMember(groupId, userId);

        if (target == null)
            throw new ApiException("not_found", "Member not found.", 404);

        if (!Enum.TryParse<GroupRole>(req.Role, true, out var newRole))
            throw new ApiException("invalid_role", "Invalid role.");

        if (target.Role == GroupRole.Admin && newRole != GroupRole.Admin)
        {
            var adminCount = await _db.Set<GroupMember>()
                .CountAsync(x => x.GroupId == groupId && x.Role == GroupRole.Admin);

            if (adminCount <= 1)
                throw new ApiException("forbidden", "Group must have at least one admin.", 403);
        }

        target.Role = newRole;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<GroupMember?> GetMember(Guid groupId, Guid userId)
    {
        return await _db.Set<GroupMember>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);
    }

    private async Task EnsureGroupMember(Guid groupId, Guid userId)
    {
        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (!isMember)
            throw new ApiException("forbidden", "You are not a member of this group.", 403);
    }
}