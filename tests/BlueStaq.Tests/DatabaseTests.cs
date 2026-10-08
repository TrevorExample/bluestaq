using BlueStaq.Domain.Entities;
using BlueStaq.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlueStaq.Tests;

public sealed class DatabaseTests : ApiTestBase
{
    [Fact]
    public async Task SQLiteEnforcesMembershipKeysAndForeignKeys()
    {
        var team = Team1;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var membership = await db.TeamMembers.AsNoTracking().SingleAsync(m => m.TeamId == team.Id);
        db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = membership.UserId });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.TeamMembers.Add(new TeamMember { TeamId = Guid.NewGuid(), UserId = membership.UserId });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
