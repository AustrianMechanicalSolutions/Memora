public record GroupListItemDto(Guid Id, string Name, int MemberCount);
public record GroupDetailDto(Guid Id, string Name, string InviteCode, int MemberCount);

public record MemoryDto(
    Guid Id,
    Guid GroupId,
    MemoryType Type,
    string? Title,
    string? QuoteText,
    string? MediaUrl,
    string? ThumbUrl,
    DateTime HappenedAt,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    List<string> Tags
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
    List<string>? Tags
);

public class MemoryQuery
{
    public MemoryType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public string Sort { get; set; } = "newest"; // newest|oldest
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
