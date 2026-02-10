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

    [HttpGet("{albumId:guid}/people")]
    public async Task<ActionResult<List<GroupMemberDto>>> AlbumPeople(Guid groupId, Guid albumId)
    {
        var uid = User.UserId();

        var album = await _db.Set<Album>()
            .Include(a => a.People)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == albumId && a.GroupId == groupId);

        if (album == null) return NotFound();

        return Ok(album.People.Select(p =>
            new GroupMemberDto(
                p.UserId,
                p.User.DisplayName,
                "Album",
                p.User.ProfileImageUrl
            )
        ));
    }

    [HttpPost("{albumId:guid}/people/{userId}")]
    public async Task<IActionResult> AddPerson(Guid groupId, Guid albumId, Guid userId)
    {
        var uid = User.UserId();
        if (!await CanEditAlbum(albumId, uid)) return Forbid();

        var exists = await _db.Set<AlbumPerson>()
            .AnyAsync(x => x.AlbumId == albumId && x.UserId == userId);

        if (!exists)
        {
            _db.Add(new AlbumPerson
            {
                AlbumId = albumId,
                UserId = userId
            });

            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{albumId:guid}/people/{userId}")]
    public async Task<IActionResult> RemovePerson(Guid groupId, Guid albumId, Guid userId)
    {
        var uid = User.UserId();
        if (!await CanEditAlbum(albumId, uid)) return Forbid();

        var entry = await _db.Set<AlbumPerson>()
            .FirstOrDefaultAsync(x => x.AlbumId == albumId && x.UserId == userId);

        if (entry == null) return NotFound();

        _db.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
}

    private async Task<bool> CanEditAlbum(Guid albumId, Guid userId)
    {
        return await _db.Set<Album>()
            .AnyAsync(a => 
                a.Id == albumId &&
                (a.CreatedByUserId == userId ||
                 a.Group.CreatedByUserId == userId ||
                 a.Group.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin))
            );
    }
}
