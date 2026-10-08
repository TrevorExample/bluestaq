namespace BlueStaq.Domain.Entities;

public class TeamMember
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public TeamRole Role { get; set; } = TeamRole.Member;
    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
