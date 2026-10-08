namespace BlueStaq.Domain.Entities;

public class Note
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Team Team { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
