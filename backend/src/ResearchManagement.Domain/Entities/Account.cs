namespace ResearchManagement.Domain.Entities;
public sealed class Account
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string Status { get; set; } = "HOAT_DONG";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
