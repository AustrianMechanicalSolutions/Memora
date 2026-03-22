using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/memories/{memoryId:guid}/comments")]
[Authorize]
public class MemoryCommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MemoryCommentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet()]
    public async Task<ActionResult<List<CommentDto>>> MemoryComments(Guid groupId, Guid memoryId)
    {
        var uid = User.UserId();
        await EnsureGroupMember(groupId, uid);

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) throw new ApiException("not_found", "Memory not found.", 404);

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
            .ToListAsync();

        var commentIds = comments.Select(x => x.c.Id).ToList();

        var likeCounts = await _db.Set<CommentLike>()
            .AsNoTracking()
            .Where(l => commentIds.Contains(l.CommentId))
            .GroupBy(l => l.CommentId)
            .Select(g => new { CommentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CommentId, x => x.Count);

        var likedIds = await _db.Set<CommentLike>()
            .AsNoTracking()
            .Where(l => commentIds.Contains(l.CommentId) && l.UserId == uid)
            .Select(l => l.CommentId)
            .ToListAsync();

        var likedSet = likedIds.ToHashSet();

        var result = comments.Select(x => new CommentDto(
            x.c.Id,
            x.c.MemoryId,
            x.c.UserId,
            x.u.DisplayName,
            x.u.ProfileImageUrl,
            x.c.Content,
            x.c.CreatedAt,
            x.c.ParentCommentId,
            likeCounts.TryGetValue(x.c.Id, out var count) ? count : 0,
            likedSet.Contains(x.c.Id)
        )).ToList();

        return Ok(result);
    }

    [HttpPost()]
    public async Task<ActionResult<CommentDto>> AddComment(Guid groupId, Guid memoryId, [FromBody] CreateCommentRequest req)
    {
        var uid = User.UserId();
        await EnsureGroupMember(groupId, uid);

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) throw new ApiException("not_found", "Memory not found.", 404);

        var content = (req.Content ?? string.Empty).Trim();
        if (content.Length > 2000)
            throw new ApiException("invalid_input", "Comment too long.");

        if (string.IsNullOrWhiteSpace(content)) throw new ApiException("invalid_input", "Comment cannot be empty.");

        if (req.ParentCommentId.HasValue)
        {
            var parentExists = await _db.Set<MemoryComment>()
                .AnyAsync(x => x.Id == req.ParentCommentId && x.MemoryId == memoryId);
            if (!parentExists) throw new ApiException("invalid_input", "Parent comment not found.");
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
            .FirstOrDefaultAsync();

        return Ok(new CommentDto(
            comment.Id,
            comment.MemoryId,
            comment.UserId,
            user!.DisplayName,
            user.ProfileImageUrl,
            comment.Content,
            comment.CreatedAt,
            comment.ParentCommentId,
            0,
            false
        ));
    }

    private async Task EnsureGroupMember(Guid groupId, Guid userId)
    {
        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (!isMember)
            throw new ApiException("forbidden", "You are not a member of this group.", 403);
    }
}