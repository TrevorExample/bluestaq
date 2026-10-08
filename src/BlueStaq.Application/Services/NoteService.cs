using BlueStaq.Application.DTOs;
using BlueStaq.Application.Interfaces;
using BlueStaq.Domain.Entities;

namespace BlueStaq.Application.Services;

public class NoteService(IAppStore store)
{
    private async Task RequireMemberAsync(Guid teamId, Guid userId, CancellationToken ct)
    {
        if (!await store.IsMemberAsync(teamId, userId, ct))
            throw new KeyNotFoundException();
    }

    private async Task<Note> RequireNoteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var note = await store.FindNoteAsync(id, ct) ?? throw new KeyNotFoundException();
        await RequireMemberAsync(note.TeamId, userId, ct);
        return note;
    }

    public async Task<IReadOnlyList<NoteResponse>> ListAsync(Guid teamId, Guid userId, CancellationToken ct)
    {
        await RequireMemberAsync(teamId, userId, ct);
        return (await store.GetNotesAsync(teamId, ct)).Select(Map).ToList();
    }

    public async Task<NoteResponse> CreateAsync(Guid teamId, Guid userId, SaveNoteRequest request, CancellationToken ct)
    {
        await RequireMemberAsync(teamId, userId, ct);
        var now = DateTimeOffset.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(), TeamId = teamId, CreatedByUserId = userId,
            Title = request.Title.Trim(), Content = request.Content, CreatedAt = now, UpdatedAt = now
        };
        await store.AddNoteAsync(note, ct);
        return Map(note);
    }

    public async Task<NoteResponse> GetAsync(Guid id, Guid userId, CancellationToken ct) =>
        Map(await RequireNoteAsync(id, userId, ct));

    public async Task<NoteResponse> UpdateAsync(Guid id, Guid userId, SaveNoteRequest request, CancellationToken ct)
    {
        var note = await RequireNoteAsync(id, userId, ct);
        note.Title = request.Title.Trim();
        note.Content = request.Content;
        note.UpdatedAt = DateTimeOffset.UtcNow;
        await store.SaveAsync(ct);
        return Map(note);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct) =>
        await store.DeleteNoteAsync(await RequireNoteAsync(id, userId, ct), ct);

    private static NoteResponse Map(Note note) => new(note.Id, note.TeamId, note.Title,
        note.Content, note.CreatedByUserId, note.CreatedAt, note.UpdatedAt);
}
