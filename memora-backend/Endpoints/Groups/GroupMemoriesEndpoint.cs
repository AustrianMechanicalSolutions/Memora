using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/memories")]
[Authorize]
public class GroupMemoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public GroupMemoriesController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet()]
    public async Task<ActionResult<object>> Memories(Guid groupId, [FromQuery] MemoryQuery q)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 20 : Math.Min(q.PageSize, 200);

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
        var pageItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var memoryIds = pageItems.Select(x => x.Id).ToList();

        var likeCounts = await _db.Set<MemoryLike>()
            .AsNoTracking()
            .Where(l => memoryIds.Contains(l.MemoryId))
            .GroupBy(l => l.MemoryId)
            .Select(g => new { MemoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MemoryId, x => x.Count);

        var commentCounts = await _db.Set<MemoryComment>()
            .AsNoTracking()
            .Where(c => memoryIds.Contains(c.MemoryId))
            .GroupBy(c => c.MemoryId)
            .Select(g => new { MemoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MemoryId, x => x.Count);

        var likedIds = await _db.Set<MemoryLike>()
            .AsNoTracking()
            .Where(l => memoryIds.Contains(l.MemoryId) && l.UserId == uid)
            .Select(l => l.MemoryId)
            .ToListAsync();
        var likedSet = likedIds.ToHashSet();

        var items = pageItems.Select(x =>
        {
            var tags = x.Tags?
                .Select(t => t.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            return new MemoryDto(
                x.Id, x.GroupId, x.Type, x.Title, x.QuoteText, x.QuoteBy, x.MediaUrl, x.ThumbUrl,
                x.HappenedAt, x.CreatedAt, x.CreatedByUserId,
                tags != null && tags.Count > 0 ? tags : null,
                x.AlbumId,
                likeCounts.TryGetValue(x.Id, out var likeCount) ? likeCount : 0,
                commentCounts.TryGetValue(x.Id, out var commentCount) ? commentCount : 0,
                likedSet.Contains(x.Id)
            );
        }).ToList();

        return Ok(new { total, items });
    }

    [HttpPost()]
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
            m.AlbumId,
            0,
            0,
            false
        ));
    }


    [HttpPost("upload")]
    [RequestSizeLimit(200_000_000)] // 200MB limit
    public async Task<ActionResult<MemoryDto>> CreateMemory(Guid groupId, [FromForm] CreateMemoryRequest req)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        string? mediaUrl = req.MediaUrl;

        if (req.File != null && req.File.Length > 0)
        {
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads");
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
            m.AlbumId,
            0,
            0,
            false
        ));
    }
}