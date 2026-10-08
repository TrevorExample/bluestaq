using System.Security.Claims;
using BlueStaq.Application.DTOs;
using BlueStaq.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueStaq.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notes")]
public class NotesController(NoteService notes) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("/api/teams/{teamId:guid}/notes")]
    public async Task<ActionResult<IReadOnlyList<NoteResponse>>> List(Guid teamId, CancellationToken ct) =>
        Ok(await notes.ListAsync(teamId, UserId, ct));

    [HttpPost("/api/teams/{teamId:guid}/notes")]
    public async Task<ActionResult<NoteResponse>> Create(Guid teamId, SaveNoteRequest request, CancellationToken ct)
    {
        var note = await notes.CreateAsync(teamId, UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = note.Id }, note);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await notes.GetAsync(id, UserId, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NoteResponse>> Update(Guid id, SaveNoteRequest request, CancellationToken ct) =>
        Ok(await notes.UpdateAsync(id, UserId, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await notes.DeleteAsync(id, UserId, ct);
        return NoContent();
    }
}
