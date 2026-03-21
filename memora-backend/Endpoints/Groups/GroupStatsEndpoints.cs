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
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var group = await _db.Set<Group>()
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { g.CreatedAt })
            .FirstOrDefaultAsync();

        if (group == null) return NotFound();

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

        return Ok(new GroupStatsDto(memoryCountTask.Result, albumCountTask.Result, group.CreatedAt));
    }
}