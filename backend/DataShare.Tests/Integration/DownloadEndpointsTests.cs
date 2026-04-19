using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DataShare.Application.DTOs;
using FluentAssertions;

namespace DataShare.Tests.Integration;

public class DownloadEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DownloadEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent BuildUpload(string fileName, string content, string? password = null)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent("1"), "expiresInDays");
        if (password != null) form.Add(new StringContent(password), "password");
        return form;
    }

    [Fact]
    public async Task GetMetadata_ExistingToken_ReturnsMetadata()
    {
        var client = _factory.CreateClient();
        var upload = await client.PostAsync("/api/files", BuildUpload("meta.txt", "hello"));
        var dto = await upload.Content.ReadFromJsonAsync<FileDto>();

        var response = await client.GetAsync($"/api/download/{dto!.DownloadToken}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metadata = await response.Content.ReadFromJsonAsync<FileMetadataDto>();
        metadata!.OriginalName.Should().Be("meta.txt");
        metadata.IsProtected.Should().BeFalse();
    }

    [Fact]
    public async Task GetMetadata_UnknownToken_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/download/unknown-token");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_ProtectedFileWithoutPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var upload = await client.PostAsync("/api/files",
            BuildUpload("protected.txt", "secret", password: "hunter2"));
        var dto = await upload.Content.ReadFromJsonAsync<FileDto>();

        var response = await client.PostAsJsonAsync(
            $"/api/download/{dto!.DownloadToken}",
            new DownloadRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Download_ProtectedFileWithCorrectPassword_ReturnsStream()
    {
        var client = _factory.CreateClient();
        var upload = await client.PostAsync("/api/files",
            BuildUpload("protected.txt", "secret-content", password: "hunter2"));
        var dto = await upload.Content.ReadFromJsonAsync<FileDto>();

        var response = await client.PostAsJsonAsync(
            $"/api/download/{dto!.DownloadToken}",
            new DownloadRequest("hunter2"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("secret-content");
    }

    [Fact]
    public async Task Download_UnknownToken_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/download/does-not-exist",
            new DownloadRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
