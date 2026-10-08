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

public sealed class NotesControllerTests : ApiTestBase
{
    [Fact]
    public async Task AnonymousRequestsAreRejected()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/notes/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task NoteLifecyclePersistsChanges()
    {
        using var client = Client();
        await Login(client);
        var team = Team1;

        var create = await client.PostAsJsonAsync($"/api/teams/{team.Id}/notes", new SaveNoteRequest("First note", "Hello"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var note = (await create.Content.ReadFromJsonAsync<NoteResponse>())!;
        Assert.Equal($"/api/notes/{note.Id}", create.Headers.Location!.AbsolutePath);
        Assert.Equal("Hello", (await client.GetFromJsonAsync<NoteResponse>($"/api/notes/{note.Id}"))!.Content);
        Assert.Single((await client.GetFromJsonAsync<List<NoteResponse>>($"/api/teams/{team.Id}/notes"))!);

        var update = await client.PutAsJsonAsync($"/api/notes/{note.Id}", new SaveNoteRequest("Updated", "Changed"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var anotherSession = Client();
        await Login(anotherSession);
        Assert.Equal("Changed", (await anotherSession.GetFromJsonAsync<NoteResponse>($"/api/notes/{note.Id}"))!.Content);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/notes/{note.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/notes/{note.Id}")).StatusCode);
    }

    [Fact]
    public async Task OtherUsersCannotReadOrModifyTeamNotes()
    {
        using var owner = Client();
        await Login(owner);
        var team = Team1;
        var noteResult = await owner.PostAsJsonAsync($"/api/teams/{team.Id}/notes", new SaveNoteRequest("Private", "Secret"));
        var note = (await noteResult.Content.ReadFromJsonAsync<NoteResponse>())!;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = Guid.NewGuid(), Email = "other@example.com", DisplayName = "Other user" };
            user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>()
                .HashPassword(user, "Test-password-123!");
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        using var other = Client();
        await Login(other, "other@example.com");
        Assert.Empty((await other.GetFromJsonAsync<List<TeamResponse>>("/api/teams"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/teams/{team.Id}/notes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/teams/{team.Id}/notes",
            new SaveNoteRequest("Attack", "Attack"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/notes/{note.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/notes/{note.Id}",
            new SaveNoteRequest("Attack", "Attack"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/notes/{note.Id}")).StatusCode);
        Assert.Equal("Secret", (await owner.GetFromJsonAsync<NoteResponse>($"/api/notes/{note.Id}"))!.Content);
    }

    [Fact]
    public async Task InvalidPayloadsAndMissingResourcesReturnExpectedStatuses()
    {
        using var client = Client();
        await Login(client);
        var team = Team1;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/teams/{team.Id}/notes",
            new SaveNoteRequest("", "Body"))).StatusCode);
        var missing = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/notes/{missing}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/notes/{missing}",
            new SaveNoteRequest("Title", "Body"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/notes/{missing}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/teams/{missing}/notes")).StatusCode);
    }

    [Fact]
    public async Task TeamMembersCanReadSharedNotes()
    {
        using var owner = Client();
        var login = await owner.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("owner@example.com", "Test-password-123!"));
        var profile = (await login.Content.ReadFromJsonAsync<UserResponse>())!;
        var first = Team1;
        await owner.PostAsJsonAsync($"/api/teams/{first.Id}/notes", new SaveNoteRequest("Shared", "Content"));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var team = await db.Teams.Include(t => t.Members).Include(t => t.Notes)
                .SingleAsync(t => t.Id == first.Id);
            Assert.Equal(TeamRole.Owner, Assert.Single(team.Members).Role);
            Assert.Equal(profile.Id, Assert.Single(team.Notes).CreatedByUserId);
            var member = new User { Id = Guid.NewGuid(), Email = "member@example.com", DisplayName = "Member" };
            member.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>()
                .HashPassword(member, "Test-password-123!");
            db.Users.Add(member);
            db.TeamMembers.Add(new TeamMember { TeamId = first.Id, UserId = member.Id });
            await db.SaveChangesAsync();
        }

        using var memberClient = Client();
        await Login(memberClient, "member@example.com");
        Assert.Equal("Shared", Assert.Single((await memberClient.GetFromJsonAsync<List<NoteResponse>>(
            $"/api/teams/{first.Id}/notes"))!).Title);
    }
}
