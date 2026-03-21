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

    [HttpGet("{groupId:guid}/memories")]
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

            var protectedMediaUrl = !string.IsNullOrWhiteSpace(x.MediaUrl)
                ? $"/api/groups/{x.GroupId}/memories/{x.Id}/media"
                : null;

            return new MemoryDto(
                x.Id, x.GroupId, x.Type, x.Title, x.QuoteText, x.QuoteBy, protectedMediaUrl, x.ThumbUrl,
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
            m.AlbumId,
            0,
            0,
            false
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

    [HttpGet("{groupId:guid}/memories/{memoryId:guid}/media")]
    public async Task<IActionResult> GetMemoryMedia(Guid groupId, Guid memoryId)
    {
        var uid = User.UserId();

        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == uid);

        if (!isMember)
            return Forbid();

        var memory = await _db.Set<Memory>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == memoryId && x.GroupId == groupId);

        if (memory == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(memory.MediaUrl))
            return NotFound();

        var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads");

        // if MediaUrl stores just the filename
        var fileName = Path.GetFileName(memory.MediaUrl);
        var filePath = Path.Combine(uploadsFolder, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = GetContentType(filePath);
        return PhysicalFile(filePath, contentType);
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    [HttpPost("{groupId:guid}/memories/{memoryId:guid}/likes")]
    public async Task<IActionResult> LikeMemory(Guid groupId, Guid memoryId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) return NotFound();

        var exists = await _db.Set<MemoryLike>()
            .AnyAsync(x => x.MemoryId == memoryId && x.UserId == uid);

        if (!exists)
        {
            _db.Add(new MemoryLike
            {
                MemoryId = memoryId,
                UserId = uid
            });
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{groupId:guid}/memories/{memoryId:guid}/likes")]
    public async Task<IActionResult> UnlikeMemory(Guid groupId, Guid memoryId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) return NotFound();

        var entry = await _db.Set<MemoryLike>()
            .FirstOrDefaultAsync(x => x.MemoryId == memoryId && x.UserId == uid);

        if (entry == null) return NotFound();

        _db.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{groupId:guid}/memories/{memoryId:guid}/comments")]
    public async Task<ActionResult<List<CommentDto>>> MemoryComments(Guid groupId, Guid memoryId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) return NotFound();

        var comments = await _db.Set<MemoryComment>()
            .AsNoTracking()
            .Where(c => c.MemoryId == memoryId)
            .Join(
                _db.Set<AppUser>(),
                c => c.UserId,
                u => u.Id,
                (c, u) => new { c, u }
            )
            .OrderBy(x => x.c.CreatedAt)
            .Select(x => new CommentDto(
                x.c.Id,
                x.c.MemoryId,
                x.c.UserId,
                x.u.DisplayName,
                x.u.ProfileImageUrl,
                x.c.Content,
                x.c.CreatedAt,
                x.c.ParentCommentId,
                _db.Set<CommentLike>().Count(l => l.CommentId == x.c.Id),
                _db.Set<CommentLike>().Any(l => l.CommentId == x.c.Id && l.UserId == uid)
            ))
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost("{groupId:guid}/memories/{memoryId:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid groupId, Guid memoryId, [FromBody] CreateCommentRequest req)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) return NotFound();

        var content = (req.Content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(content)) return BadRequest("Comment cannot be empty.");

        if (req.ParentCommentId.HasValue)
        {
            var parentExists = await _db.Set<MemoryComment>()
                .AnyAsync(x => x.Id == req.ParentCommentId && x.MemoryId == memoryId);
            if (!parentExists) return BadRequest("Parent comment not found.");
        }

        var comment = new MemoryComment
        {
            Id = Guid.NewGuid(),
            MemoryId = memoryId,
            UserId = uid,
            Content = content,
            ParentCommentId = req.ParentCommentId
        };

        _db.Add(comment);
        await _db.SaveChangesAsync();

        var user = await _db.Set<AppUser>()
            .AsNoTracking()
            .Where(u => u.Id == uid)
            .Select(u => new { u.DisplayName, u.ProfileImageUrl })
            .FirstAsync();

        return Ok(new CommentDto(
            comment.Id,
            comment.MemoryId,
            comment.UserId,
            user.DisplayName,
            user.ProfileImageUrl,
            comment.Content,
            comment.CreatedAt,
            comment.ParentCommentId,
            0,
            false
        ));
    }

    [HttpPost("{groupId:guid}/comments/{commentId:guid}/likes")]
    public async Task<IActionResult> LikeComment(Guid groupId, Guid commentId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var comment = await _db.Set<MemoryComment>()
            .AsNoTracking()
            .Join(
                _db.Set<Memory>(),
                c => c.MemoryId,
                m => m.Id,
                (c, m) => new { c, m }
            )
            .FirstOrDefaultAsync(x => x.c.Id == commentId && x.m.GroupId == groupId);

        if (comment == null) return NotFound();

        var exists = await _db.Set<CommentLike>()
            .AnyAsync(x => x.CommentId == commentId && x.UserId == uid);

        if (!exists)
        {
            _db.Add(new CommentLike
            {
                CommentId = commentId,
                UserId = uid
            });
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{groupId:guid}/comments/{commentId:guid}/likes")]
    public async Task<IActionResult> UnlikeComment(Guid groupId, Guid commentId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var commentInGroup = await _db.Set<MemoryComment>()
            .AsNoTracking()
            .Join(
                _db.Set<Memory>(),
                c => c.MemoryId,
                m => m.Id,
                (c, m) => new { c, m }
            )
            .AnyAsync(x => x.c.Id == commentId && x.m.GroupId == groupId);

        if (!commentInGroup) return NotFound();

        var entry = await _db.Set<CommentLike>()
            .FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == uid);

        if (entry == null) return NotFound();

        _db.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
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

    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
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

    [HttpPut("{groupId:guid}/members/{userId:guid}/role")]
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
