using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/stats")]
[Authorize]
public class GroupStatsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GroupStatsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet()]
    public async Task<ActionResult<GroupStatsDto>> Stats(Guid groupId)
    {
        var uid = User.UserId();
        await EnsureGroupMember(groupId, uid);

        var group = await _db.Set<Group>()
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { g.CreatedAt })
            .FirstOrDefaultAsync();

        if (group == null) throw new ApiException("not_found", "Group not found.", 404);

        var memoryCountTask = _db.Set<Memory>()
            .AsNoTracking()
            .CountAsync(m => m.GroupId == groupId);

        var albumCountTask = _db.Set<Memory>()
            .AsNoTracking()
            .Where(m => m.GroupId == groupId && m.AlbumId != null)
            .Select(m => m.AlbumId)
            .Distinct()
            .CountAsync();

        await Task.WhenAll(memoryCountTask, albumCountTask);

        var memoryCount = await memoryCountTask;
        var albumCount = await albumCountTask;

        return Ok(new GroupStatsDto(memoryCount, albumCount, group.CreatedAt));
    }

    private async Task EnsureGroupMember(Guid groupId, Guid userId)
    {
        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (!isMember)
            throw new ApiException("forbidden", "You are not a member of this group.", 403);
    }
}