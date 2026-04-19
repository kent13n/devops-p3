using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DataShare.Application.DTOs;
using FluentAssertions;

namespace DataShare.Tests.Integration;

public class FileEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public FileEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent BuildUpload(string fileName, string content, int? expiresInDays = 1, string? password = null, string? tags = null)
    {
        var contentType = fileName.EndsWith(".txt") ? "text/plain"
                        : fileName.EndsWith(".exe") ? "application/x-msdownload"
                        : "application/octet-stream";

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);
        if (expiresInDays.HasValue) form.Add(new StringContent(expiresInDays.Value.ToString()), "expiresInDays");
        if (password != null) form.Add(new StringContent(password), "password");
        if (tags != null) form.Add(new StringContent(tags), "tags");
        return form;
    }

    private async Task<string> RegisterAndGetTokenAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = "Password123!" });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    [Fact]
    public async Task PostFiles_AnonymousTxt_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/files", BuildUpload("hello.txt", "hello world"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<FileDto>();
        dto!.OriginalName.Should().Be("hello.txt");
        dto.DownloadToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostFiles_BlockedExtension_ReturnsUnsupportedMediaType()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/files", BuildUpload("virus.exe", "MZ"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task PostFiles_AuthenticatedWithTags_PersistsTags()
    {
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/files",
            BuildUpload("tagged.txt", "data", tags: "work,urgent"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<FileDto>();
        dto!.Tags.Should().BeEquivalentTo(new[] { "work", "urgent" });
    }

    [Fact]
    public async Task GetFiles_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/files");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFiles_Authenticated_ReturnsUserFiles()
    {
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsync("/api/files", BuildUpload("a.txt", "A"));
        await client.PostAsync("/api/files", BuildUpload("b.txt", "B"));

        var response = await client.GetAsync("/api/files");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var files = await response.Content.ReadFromJsonAsync<FileDto[]>();
        files!.Length.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetFiles_InvalidStatus_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/files?status=banana");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteFile_Owner_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var upload = await client.PostAsync("/api/files", BuildUpload("owned.txt", "owned"));
        var dto = await upload.Content.ReadFromJsonAsync<FileDto>();

        var response = await client.DeleteAsync($"/api/files/{dto!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteFile_OtherUser_ReturnsNotFoundIndistinguishable()
    {
        var ownerClient = _factory.CreateClient();
        var ownerToken = await RegisterAndGetTokenAsync(ownerClient);
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var upload = await ownerClient.PostAsync("/api/files", BuildUpload("victim.txt", "owned"));
        var dto = await upload.Content.ReadFromJsonAsync<FileDto>();

        var attackerClient = _factory.CreateClient();
        var attackerToken = await RegisterAndGetTokenAsync(attackerClient);
        attackerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        var response = await attackerClient.DeleteAsync($"/api/files/{dto!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
