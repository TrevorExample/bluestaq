using BlueStaq.Application.DTOs;
using BlueStaq.Application.Interfaces;
using BlueStaq.Domain.Entities;

namespace BlueStaq.Application.Services;

public class TeamService(IAppStore store)
{
    public async Task<IReadOnlyList<TeamResponse>> GetTeamsAsync(Guid userId, CancellationToken ct) =>
        (await store.GetTeamsAsync(userId, ct)).Select(t => new TeamResponse(t.Id, t.Name, t.CreatedAt)).ToList();

    public async Task<TeamResponse> CreateAsync(Guid userId, CreateTeamRequest request, CancellationToken ct)
    {
        var team = new Team { Id = Guid.NewGuid(), Name = request.Name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
        var member = new TeamMember { TeamId = team.Id, UserId = userId, Role = TeamRole.Owner };
        await store.CreateTeamAsync(team, member, ct);
        return new(team.Id, team.Name, team.CreatedAt);
    }
}
