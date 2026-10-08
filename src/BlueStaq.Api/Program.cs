using System.Threading.RateLimiting;
using BlueStaq.Application.Interfaces;
using BlueStaq.Application.Services;
using BlueStaq.Domain.Entities;
using BlueStaq.Infrastructure.Data;
using BlueStaq.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=bluestaq.db"));
builder.Services.AddScoped<IAppStore, AppStore>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "BlueStaq.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var email = builder.Configuration["BootstrapUser:Email"]?.Trim().ToLowerInvariant();
    var password = builder.Configuration["BootstrapUser:Password"];
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)
        && !await db.Users.AnyAsync(u => u.Email == email))
    {
        if (password.Length < 12)
            throw new InvalidOperationException("Bootstrap user password must contain at least 12 characters.");
        var displayName = builder.Configuration["BootstrapUser:DisplayName"]?.Trim() ?? email;
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
            throw new InvalidOperationException("Bootstrap user display name must contain 1 to 100 characters.");
        var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = displayName };
        user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (KeyNotFoundException)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new { message = "Resource not found." });
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();

public partial class Program;
