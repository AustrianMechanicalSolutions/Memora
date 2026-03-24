using AuthApi.Data;
using AuthApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult Error(string code, string message, int status = 400)
    {
        return StatusCode(status, new ApiError(code, message));
    }

    protected async Task EnsureGroupMember(AppDbContext _db, Guid groupId, Guid userId)
    {
        var isMember = await _db.Set<GroupMember>()
            .AnyAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (!isMember)
            throw new ApiException("forbidden", "You are not a member of this group.", 403);
    }
}