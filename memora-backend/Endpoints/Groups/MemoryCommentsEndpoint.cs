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

    [HttpPost()]
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
}