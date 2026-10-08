using System.Net;
using System.Net.Http.Json;
using BlueStaq.Application.DTOs;
using Xunit;

namespace BlueStaq.Tests;

public sealed class AuthControllerTests : ApiTestBase
{
    [Fact]
    public async Task LoginReturnsUserProfile()
    {
        using var client = Client();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("owner@example.com", "Test-password-123!"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.Equal("Team owner", profile.DisplayName);
    }

    [Fact]
    public async Task InvalidCredentialsAreRejected()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("owner@example.com", "incorrect"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("missing@example.com", "incorrect"))).StatusCode);
    }
}
