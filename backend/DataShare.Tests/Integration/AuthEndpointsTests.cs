using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataShare.Application.DTOs;
using FluentAssertions;

namespace DataShare.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@test.local";

    [Fact]
    public async Task Register_ValidCredentials_ReturnsCreatedWithJwt()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = UniqueEmail(), Password = "Password123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        var first = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = "Password123!" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = "Password123!" });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithJwt()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = password });

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = "Password123!" });

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidJwt_ReturnsOkWithUser()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = "Password123!" });
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var me = await client.GetAsync("/api/auth/me");

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await me.Content.ReadFromJsonAsync<UserDto>();
        dto!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Me_WithoutJwt_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
