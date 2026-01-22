using AuthApi.Data;
using AuthApi.Extensions;
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

        return Ok(new GroupDetailDto(g.Id, g.Name, g.InviteCode, g.Members.Count));
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

        return Ok(new GroupDetailDto(g.Id, g.Name, g.InviteCode, g.Members.Count));
    }

    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<GroupDetailDto>> Detail(Guid groupId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var g = await _db.Set<Group>().Include(x => x.Members).FirstAsync(x => x.Id == groupId);
        return Ok(new GroupDetailDto(g.Id, g.Name, g.InviteCode, g.Members.Count));
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
                x.Id, x.GroupId, x.Type, x.Title, x.QuoteText, x.MediaUrl, x.ThumbUrl,
                x.HappenedAt, x.CreatedAt, x.CreatedByUserId,
                x.Tags.Select(t => t.Value).ToList()
            ))
            .ToListAsync();

        return Ok(new { total, items });
    }

    [HttpPost("{groupId:guid}/memories")]
    public async Task<ActionResult<MemoryDto>> CreateMemory(Guid groupId, [FromBody] CreateMemoryRequest req)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var m = new Memory
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Type = req.Type,
            Title = req.Title,
            QuoteText = req.QuoteText,
            MediaUrl = req.MediaUrl,
            ThumbUrl = req.ThumbUrl,
            HappenedAt = req.HappenedAt,
            CreatedByUserId = uid
        };

        if (req.Tags?.Any() == true)
            m.Tags = req.Tags.Distinct().Select(t => new MemoryTag { MemoryId = m.Id, Value = t.Trim() }).ToList();

        _db.Add(m);
        await _db.SaveChangesAsync();

        return Ok(new MemoryDto(
            m.Id, m.GroupId, m.Type, m.Title, m.QuoteText, m.MediaUrl, m.ThumbUrl,
            m.HappenedAt, m.CreatedAt, m.CreatedByUserId,
            m.Tags.Select(t => t.Value).ToList()
        ));
    }
}
