using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/activity")]
[Authorize]
public class GroupActivityController : ControllerBase
{
    private readonly AppDbContext _db;

    public GroupActivityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("week")]
    public async Task<ActionResult<GroupWeeklyActivityDto>> WeeklyActivity(Guid groupId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == uid);

        if (!isMember) return Forbid();

        var since = DateTime.UtcNow.AddDays(-7);

        var memories = await _db.Set<Memory>()
            .AsNoTracking()
            .Where(m => m.GroupId == groupId && m.CreatedAt >= since)
            .ToListAsync();

        var photos = memories.Count(m => m.Type == MemoryType.Photo);
        var videos = memories.Count(m => m.Type == MemoryType.Video);
        var quotes = memories.Count(m => m.Type == MemoryType.Quote);

        var albums = memories
            .Where(m => m.AlbumId != null)
            .Select(m => m.AlbumId)
            .Distinct()
            .Count();

        var contributorIds = memories
            .Select(m => m.CreatedByUserId)
            .Distinct()
            .Take(5)
            .ToList();

        var contributors = await _db.Set<AppUser>()
            .AsNoTracking()
            .Where(u => contributorIds.Contains(u.Id))
            .Select(u => new GroupWeeklyContributorDto(
                u.Id,
                u.DisplayName,
                u.ProfileImageUrl
            ))
            .ToListAsync();

        return Ok(new GroupWeeklyActivityDto(
            photos,
            videos,
            quotes,
            albums,
            contributors
        ));
    }

    [HttpGet("members")]
    public async Task<ActionResult<List<GroupMemberActivityDto>>> MemberActivity(Guid groupId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == uid);

        if (!isMember) return Forbid();

        var members = await _db.Set<GroupMember>()
            .AsNoTracking()
            .Where(gm => gm.GroupId == groupId)
            .Join(
                _db.Set<AppUser>(),
                gm => gm.UserId,
                u => u.Id,
                (gm, u) => new { gm, u}
            )
            .ToListAsync();

        var memories = await _db.Set<Memory>()
            .AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .ToListAsync();

        var result = members.Select(m =>
        {
            var userMemories = memories.Where(x => x.CreatedByUserId == m.u.Id).ToList();

            return new GroupMemberActivityDto(
                m.u.Id,
                m.u.DisplayName,
                m.gm.Role.ToString(),
                m.gm.JoinedAt,
                userMemories.Any() ? userMemories.Max(x => x.CreatedAt) : null,
                m.u.ProfileImageUrl,
                userMemories.Count,
                userMemories.Count(x => x.Type == MemoryType.Photo),
                userMemories.Count(x => x.Type == MemoryType.Video),
                userMemories.Count(x => x.Type == MemoryType.Quote)
            );
        }).ToList();

        return Ok(result);
    }
}