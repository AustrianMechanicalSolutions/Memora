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

    [HttpDelete()]
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
}