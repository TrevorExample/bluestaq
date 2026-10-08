using System.Net;
using System.Net.Http.Json;
using BlueStaq.Application.DTOs;
using BlueStaq.Domain.Entities;
using BlueStaq.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlueStaq.Tests;

public sealed class TeamsControllerTests : ApiTestBase
{
    [Fact]
    public async Task AnonymousRequestsAreRejected()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/teams")).StatusCode);
    }

    [Fact]
    public async Task CreatedTeamAppearsInList()
    {
        using var client = Client();
        await Login(client);
        var response = await client.PostAsJsonAsync("/api/teams", new CreateTeamRequest("My team"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var team = (await response.Content.ReadFromJsonAsync<TeamResponse>())!;
        Assert.NotEqual(default, team.CreatedAt);
        var teams = await client.GetFromJsonAsync<List<TeamResponse>>("/api/teams");
        Assert.Contains(teams!, t => t.Id == team.Id);
        Assert.Contains(teams!, t => t.Id == Team1.Id && t.Name == "Team1");
    }

    [Fact]
    public async Task InvalidTeamNameIsRejected()
    {
        using var client = Client();
        await Login(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest(" "))).StatusCode);
    }

    [Fact]
    public async Task UsersCanJoinMultipleTeams()
    {
        using var owner = Client();
        await Login(owner);
        var first = Team1;
        await owner.PostAsJsonAsync("/api/teams", new CreateTeamRequest("Second"));
        Assert.Equal(2, (await owner.GetFromJsonAsync<List<TeamResponse>>("/api/teams"))!.Count);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var team = await db.Teams.Include(t => t.Members).SingleAsync(t => t.Id == first.Id);
            Assert.Equal(TeamRole.Owner, Assert.Single(team.Members).Role);
            var member = new User { Id = Guid.NewGuid(), Email = "member@example.com", DisplayName = "Member" };
            member.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>()
                .HashPassword(member, "Test-password-123!");
            db.Users.Add(member);
            db.TeamMembers.Add(new TeamMember { TeamId = first.Id, UserId = member.Id });
            await db.SaveChangesAsync();
        }

        using var memberClient = Client();
        await Login(memberClient, "member@example.com");
        Assert.Equal(first.Id, Assert.Single((await memberClient.GetFromJsonAsync<List<TeamResponse>>("/api/teams"))!).Id);
    }
}
