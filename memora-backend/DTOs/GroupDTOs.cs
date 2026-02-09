public record GroupListItemDto(Guid Id, string Name, int MemberCount);
public record GroupDetailDto(Guid Id, string Name, string InviteCode, int MemberCount, Guid CreatedByUserId);
public record GroupMemberDto(Guid UserId, string Name, string Role, string? AvatarUrl);

public record GroupStatsDto(int memoryCount, int albumCount, DateTime timeActive);

public record MemoryDto(
    Guid Id,
    Guid GroupId,
    MemoryType Type,
    string? Title,
    string? QuoteText,
    string? QuoteBy,
    string? MediaUrl,
    string? ThumbUrl,
    DateTime HappenedAt,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    List<string>? Tags,
    Guid? AlbumId
);

public record CreateGroupRequest(string Name);
public record JoinGroupRequest(string InviteCode);
public record CreateMemoryRequest(
    MemoryType Type,
    string? Title,
    string? QuoteText,
    string? MediaUrl,
    string? ThumbUrl,
    DateTime HappenedAt,
    List<string>? Tags,
    IFormFile? File,
    Guid? AlbumId
);

public class CreateQuoteRequest
{
    public string? Title { get; set; }
    public string QuoteText { get; set; } = "";
    public string? QuoteBy { get; set; }
    public DateTime HappenedAt { get; set; }
    public List<string>? Tags { get; set; }
    public Guid? AlbumId { get; set; }
};

public class MemoryQuery
{
    public MemoryType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public string Sort { get; set; } = "newest"; // newest|oldest
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? AlbumId { get; set; }
}

public record GroupWeeklyActivityDto(
    int Photos,
    int Videos,
    int Quotes,
    int Albums,
    List<string> Contributors
);

public record GroupMemberActivityDto(
    Guid UserId,
    string Name,
    string Role,
    DateTime JoinedAt,
    DateTime? LastActiveAt,
    int TotalMemories,
    int PhotoCount,
    int VideoCount,
    int QuoteCount
);
