using BlueStaq.Application.Interfaces;
using BlueStaq.Domain.Entities;
using BlueStaq.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlueStaq.Infrastructure.Repositories;

public class AppStore(AppDbContext db) : IAppStore
{
    public Task<User?> FindUserAsync(string email, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
    public async Task<IReadOnlyList<Team>> GetTeamsAsync(Guid userId, CancellationToken ct) =>
        await db.Teams.AsNoTracking().Where(t => db.TeamMembers.Any(m => m.TeamId == t.Id && m.UserId == userId))
            .OrderBy(t => t.Name).ToListAsync(ct);
    public Task<bool> IsMemberAsync(Guid teamId, Guid userId, CancellationToken ct) =>
        db.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
    public async Task CreateTeamAsync(Team team, TeamMember member, CancellationToken ct)
    {
        db.Teams.Add(team);
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<Note>> GetNotesAsync(Guid teamId, CancellationToken ct) =>
        await db.Notes.AsNoTracking().Where(n => n.TeamId == teamId).OrderBy(n => n.Title).ThenBy(n => n.Id).ToListAsync(ct);
    public Task<Note?> FindNoteAsync(Guid id, CancellationToken ct) => db.Notes.SingleOrDefaultAsync(n => n.Id == id, ct);
    public async Task AddNoteAsync(Note note, CancellationToken ct)
    {
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);
    }
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
    public async Task DeleteNoteAsync(Note note, CancellationToken ct)
    {
        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);
    }
}
