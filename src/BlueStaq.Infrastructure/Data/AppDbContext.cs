using BlueStaq.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlueStaq.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasIndex(u => u.Email).IsUnique();
        model.Entity<User>().Property(u => u.Email).HasMaxLength(254);
        model.Entity<User>().Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        model.Entity<Team>().Property(t => t.Name).HasMaxLength(100);
        model.Entity<TeamMember>().HasKey(m => new { m.TeamId, m.UserId });
        model.Entity<TeamMember>().Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        model.Entity<TeamMember>().HasOne(m => m.Team).WithMany(t => t.Members).HasForeignKey(m => m.TeamId);
        model.Entity<TeamMember>().HasOne(m => m.User).WithMany(u => u.TeamMemberships).HasForeignKey(m => m.UserId);
        model.Entity<Note>().HasOne(n => n.Team).WithMany(t => t.Notes).HasForeignKey(n => n.TeamId);
        model.Entity<Note>().HasOne(n => n.CreatedByUser).WithMany(u => u.CreatedNotes).HasForeignKey(n => n.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        model.Entity<Note>().Property(n => n.Title).HasMaxLength(200);
        model.Entity<Note>().Property(n => n.Content).HasMaxLength(50000);
    }
}
