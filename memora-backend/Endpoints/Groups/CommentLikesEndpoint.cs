using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/comments/{commentId:guid}/likes")]
[Authorize]
public class CommentLikesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentLikesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost()]
    public async Task<IActionResult> LikeComment(Guid groupId, Guid commentId)
    {
        var uid = User.UserId();
        await EnsureGroupMember(groupId, uid);

        var comment = await _db.Set<MemoryComment>()
            .AsNoTracking()
            .Join(
                _db.Set<Memory>(),
                c => c.MemoryId,
                m => m.Id,
                (c, m) => new { c, m }
            )
            .FirstOrDefaultAsync(x => x.c.Id == commentId && x.m.GroupId == groupId);

        if (comment == null) throw new ApiException("not_found", "Comment not found.", 404);

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

    [HttpDelete()]
    public async Task<IActionResult> UnlikeComment(Guid groupId, Guid commentId)
    {
        var uid = User.UserId();
        await EnsureGroupMember(groupId, uid);

        var commentInGroup = await _db.Set<MemoryComment>()
            .AsNoTracking()
            .Join(
                _db.Set<Memory>(),
                c => c.MemoryId,
                m => m.Id,
                (c, m) => new { c, m }
            )
            .AnyAsync(x => x.c.Id == commentId && x.m.GroupId == groupId);

        if (!commentInGroup) throw new ApiException("not_found", "Comment not found.", 404);

        var entry = await _db.Set<CommentLike>()
            .FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == uid);

        if (entry == null) throw new ApiException("not_found", "Comment like entry not found.", 404);

        _db.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task EnsureGroupMember(Guid groupId, Guid userId)
    {
        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (!isMember)
            throw new ApiException("forbidden", "You are not a member of this group.", 403);
    }
}