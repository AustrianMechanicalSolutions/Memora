namespace AuthApi.Dtos;

public record AlbumDto(
    Guid Id,
    Guid GroupId,
    string Title,
    string? Description,
    DateTime DateStart,
    DateTime? DateEnd,
    int MemoryCount
);

public record CreateAlbumRequest(
    string Title,
    string? Description,
    DateTime DateStart,
    DateTime? DateEnd
);