using BlueStaq.Domain.Entities;

namespace BlueStaq.Application.Interfaces;

public interface IAppStore
{
    Task<User?> FindUserAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<Team>> GetTeamsAsync(Guid userId, CancellationToken ct);
    Task<bool> IsMemberAsync(Guid teamId, Guid userId, CancellationToken ct);
    Task CreateTeamAsync(Team team, TeamMember member, CancellationToken ct);
    Task<IReadOnlyList<Note>> GetNotesAsync(Guid teamId, CancellationToken ct);
    Task<Note?> FindNoteAsync(Guid id, CancellationToken ct);
    Task AddNoteAsync(Note note, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task DeleteNoteAsync(Note note, CancellationToken ct);
}
