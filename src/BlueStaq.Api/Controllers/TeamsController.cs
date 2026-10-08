using System.Security.Claims;
using BlueStaq.Application.DTOs;
using BlueStaq.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueStaq.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/teams")]
public class TeamsController(TeamService teams) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamResponse>>> Get(CancellationToken ct) =>
        Ok(await teams.GetTeamsAsync(UserId, ct));

    [HttpPost]
    public async Task<ActionResult<TeamResponse>> Create(CreateTeamRequest request, CancellationToken ct)
    {
        var team = await teams.CreateAsync(UserId, request, ct);
        return Created("/api/teams", team);
    }
}
