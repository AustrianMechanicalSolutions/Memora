using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _db;
    public GroupsController(AppDbContext db) => _db = db;

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
        return Ok(new GroupDetailDto(g.Id, g.Name, g.InviteCode, g.Members.Count, g.CreatedByUserId));
    }

    [HttpGet("{groupId:guid}/memories")]
    public async Task<ActionResult<object>> Memories(Guid groupId, [FromQuery] MemoryQuery q)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var query = _db.Set<Memory>()
            .AsNoTracking()
            .Include(x => x.Tags)
            .Where(x => x.GroupId == groupId);

        if (q.AlbumId.HasValue) query = query.Where(x => x.AlbumId == q.AlbumId.Value);
        if (q.Type.HasValue) query = query.Where(x => x.Type == q.Type.Value);
        if (q.From.HasValue) query = query.Where(x => x.HappenedAt >= q.From.Value);
        if (q.To.HasValue) query = query.Where(x => x.HappenedAt <= q.To.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Title ?? "").ToLower().Contains(s) ||
                (x.QuoteText ?? "").ToLower().Contains(s) ||
                x.Tags.Any(t => t.Value.ToLower().Contains(s))
            );
        }

        query = q.Sort == "oldest"
            ? query.OrderBy(x => x.HappenedAt).ThenBy(x => x.CreatedAt)
            : query.OrderByDescending(x => x.HappenedAt).ThenByDescending(x => x.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(x => new MemoryDto(
                x.Id, x.GroupId, x.Type, x.Title, x.QuoteText, x.QuoteBy, x.MediaUrl, x.ThumbUrl,
                x.HappenedAt, x.CreatedAt, x.CreatedByUserId,
                x.Tags
                    .Any()
                    ? x.Tags
                        .Select(t => t.Value)
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .ToList()
                    : null,
                x.AlbumId
            ))
            .ToListAsync();

        return Ok(new { total, items });
    }

    [HttpPost("{groupId:guid}/memories")]
    public async Task<ActionResult<MemoryDto>> CreateQuote(Guid groupId, [FromBody] CreateQuoteRequest req)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var m = new Memory
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Type = MemoryType.Quote,
            Title = req.Title,
            QuoteText = req.QuoteText,
            QuoteBy = req.QuoteBy,
            HappenedAt = req.HappenedAt,
            CreatedByUserId = uid,
            AlbumId = req.AlbumId,
        };

        if (req.Tags?.Any() == true)
        {
            var cleanTags = req.Tags
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            m.Tags = cleanTags.Select(t => new MemoryTag { MemoryId = m.Id, Value = t }).ToList();
        }

        _db.Add(m);
        await _db.SaveChangesAsync();

        var tags = m.Tags.Select(t => t.Value).ToList();

        return Ok(new MemoryDto(
            m.Id, m.GroupId, m.Type, m.Title, m.QuoteText, m.QuoteBy, m.MediaUrl, m.ThumbUrl,
            m.HappenedAt, m.CreatedAt, m.CreatedByUserId,
            tags.Count == 0 ? null : tags,
            m.AlbumId
        ));
    }


    [HttpPost("{groupId:guid}/memories/upload")]
    [RequestSizeLimit(200_000_000)] // 200MB limit
    public async Task<ActionResult<MemoryDto>> CreateMemory(Guid groupId, [FromForm] CreateMemoryRequest req)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        string? mediaUrl = req.MediaUrl;

        if (req.File != null && req.File.Length > 0)
        {
            var uploadsFolder = Path.Combine("wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(req.File.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = System.IO.File.Create(filePath);
            await req.File.CopyToAsync(stream);

            mediaUrl = $"/uploads/{fileName}";
        }

        var m = new Memory
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Type = req.Type,
            Title = req.Title,
            QuoteText = req.QuoteText,
            MediaUrl = mediaUrl,
            ThumbUrl = req.ThumbUrl,
            HappenedAt = req.HappenedAt,
            CreatedByUserId = uid,
            AlbumId = req.AlbumId,
        };

        if (req.Tags?.Any() == true)
        {
            var cleanTags = req.Tags
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            m.Tags = cleanTags
                .Select(t => new MemoryTag { MemoryId = m.Id, Value = t })
                .ToList();
        }

        _db.Add(m);
        await _db.SaveChangesAsync();

        return Ok(new MemoryDto(
            m.Id, m.GroupId, m.Type, m.Title, m.QuoteText, m.QuoteBy, m.MediaUrl, m.ThumbUrl,
            m.HappenedAt, m.CreatedAt, m.CreatedByUserId,
            m.Tags.Select(t => t.Value).ToList(),
            m.AlbumId
        ));
    }

    [HttpGet("{groupId:guid}/members")]
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

    [HttpGet("{groupId:guid}/stats")]
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

    [HttpGet("{groupId:guid}/activity/week")]
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

        var contributors = await _db.Set<AppUser>()
            .Where(u => memories.Select(m => m.CreatedByUserId).Contains(u.Id))
            .Select(u => u.DisplayName)
            .Distinct()
            .Take(5)
            .ToListAsync();

        return Ok(new GroupWeeklyActivityDto(
            photos,
            videos,
            quotes,
            albums,
            contributors
        ));
    }

    [HttpGet("{groupId:guid}/activity/members")]
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
                userMemories.Count,
                userMemories.Count(x => x.Type == MemoryType.Photo),
                userMemories.Count(x => x.Type == MemoryType.Video),
                userMemories.Count(x => x.Type == MemoryType.Quote)
            );
        }).ToList();

        return Ok(result);
    }
}
