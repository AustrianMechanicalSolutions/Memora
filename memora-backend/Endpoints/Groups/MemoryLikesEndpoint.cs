using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/groups/{groupId:guid}/memories/{memoryId:guid}/likes")]
[Authorize]
public class MemoryLikesController : BaseApiController {

    private readonly AppDbContext _db;

    public MemoryLikesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost()]
    public async Task<IActionResult> LikeMemory(Guid groupId, Guid memoryId)
    {
        var uid = User.UserId();
        await EnsureGroupMember(_db, groupId, uid);

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) throw new ApiException("not_found", "Memory not found.", 404);

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
        await EnsureGroupMember(_db, groupId, uid);

        var memoryExists = await _db.Set<Memory>().AnyAsync(x => x.Id == memoryId && x.GroupId == groupId);
        if (!memoryExists) throw new ApiException("not_found", "Memory not found.", 404);

        var entry = await _db.Set<MemoryLike>()
            .FirstOrDefaultAsync(x => x.MemoryId == memoryId && x.UserId == uid);

        if (entry == null) throw new ApiException("not_found", "Like not found.", 404);

        _db.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}