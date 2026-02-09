using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthApi.Dtos;

namespace AuthApi.Endpoints;

[ApiController]
[Route("api/groups/{groupId:guid}/albums")]
[Authorize]
public class AlbumEndpoint : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    public AlbumEndpoint(AppDbContext db, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [HttpGet]
    public async Task<ActionResult<List<AlbumDto>>> Albums(Guid groupId)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var albums = await _db.Set<Album>()
            .AsNoTracking()
            .Where(a => a.GroupId == groupId)
            .OrderByDescending(a => a.DateStart)
            .Select(a => new AlbumDto(
                a.Id,
                a.GroupId,
                a.Title,
                a.Description,
                a.DateStart,
                a.DateEnd,
                a.Memories.Count
            ))
            .ToListAsync();

        return Ok(albums);
    }

    [HttpPost]
    public async Task<ActionResult<AlbumDto>> CreateAlbum(Guid groupId, [FromBody] CreateAlbumRequest req)
    {
        var uid = User.UserId();
        var isMember = await _db.Set<GroupMember>().AnyAsync(x => x.GroupId == groupId && x.UserId == uid);
        if (!isMember) return Forbid();

        var album = new Album
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Title = req.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            DateStart = req.DateStart,
            DateEnd = req.DateEnd,
            CreatedByUserId = uid,
        };

        _db.Add(album);
        await _db.SaveChangesAsync();

        return Ok(new AlbumDto(
            album.Id,
            album.GroupId,
            album.Title,
            album.Description,
            album.DateStart,
            album.DateEnd,
            0
        ));
    }
}
