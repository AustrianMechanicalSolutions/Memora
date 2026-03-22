using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/memories/{memoryId:guid}/likes")]
[Authorize]
public class MemoryLikesController : ControllerBase {

    private readonly AppDbContext _db;

    public MemoryLikesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost()]
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

    [HttpDelete()]
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
}