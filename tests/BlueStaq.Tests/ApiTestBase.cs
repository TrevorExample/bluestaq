using System.Net;
using System.Net.Http.Json;
using BlueStaq.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BlueStaq.Tests;

public abstract class ApiTestBase : IDisposable, IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"bluestaq-{Guid.NewGuid()}.db");
    protected readonly WebApplicationFactory<Program> factory;
    protected TeamResponse Team1 { get; private set; } = null!;

    protected ApiTestBase()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={databasePath};Pooling=False");
            builder.UseSetting("BootstrapUser:Email", "owner@example.com");
            builder.UseSetting("BootstrapUser:DisplayName", "Team owner");
            builder.UseSetting("BootstrapUser:Password", "Test-password-123!");
        });
    }

    protected HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public async Task InitializeAsync()
    {
        using var owner = Client();
        await Login(owner);
        var response = await owner.PostAsJsonAsync("/api/teams", new CreateTeamRequest("Team1"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Team1 = (await response.Content.ReadFromJsonAsync<TeamResponse>())!;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected static async Task Login(HttpClient client, string email = "owner@example.com")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Test-password-123!"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        factory.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
