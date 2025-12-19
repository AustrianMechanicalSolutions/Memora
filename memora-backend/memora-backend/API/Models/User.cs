namespace Memora.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
}
