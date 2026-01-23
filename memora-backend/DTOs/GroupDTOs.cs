public record GroupListItemDto(Guid Id, string Name, int MemberCount);
public record GroupDetailDto(Guid Id, string Name, string InviteCode, int MemberCount, Guid CreatedByUserId);

public record GroupMemberDto(Guid UserId, string Name, string Role);

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
    List<string>? Tags 
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
    IFormFile? File
);

public class CreateQuoteRequest
{
    public string? Title { get; set; }
    public string QuoteText { get; set; } = "";
    public string? QuoteBy { get; set; }
    public DateTime HappenedAt { get; set; }
    public List<string>? Tags { get; set; }
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
}

