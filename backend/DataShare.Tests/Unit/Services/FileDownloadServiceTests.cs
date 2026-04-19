using DataShare.Application.Interfaces;
using DataShare.Application.Services;
using DataShare.Domain.Entities;
using AwesomeAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace DataShare.Tests.Unit.Services;

public class FileDownloadServiceTests
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IFilePasswordHasher _hasher;
    private readonly FileDownloadService _service;

    public FileDownloadServiceTests()
    {
        _db = Substitute.For<IApplicationDbContext>();
        _storage = Substitute.For<IFileStorageService>();
        _hasher = Substitute.For<IFilePasswordHasher>();
        _service = new FileDownloadService(_db, _storage, _hasher);
    }

    private void SetupFiles(params StoredFile[] files)
    {
        var set = files.ToList().BuildMockDbSet();
        _db.StoredFiles.Returns(set);
    }

    private static StoredFile File(string token, DateTime? expiresAt = null, string? passwordHash = null) => new()
    {
        Id = Guid.NewGuid(),
        OriginalName = "test.txt",
        MimeType = "text/plain",
        SizeBytes = 10,
        StoragePath = "path.bin",
        DownloadToken = token,
        PasswordHash = passwordHash,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetMetadataAsync_UnknownToken_ReturnsNotFound()
    {
        SetupFiles();

        var (metadata, error) = await _service.GetMetadataAsync("unknown");

        metadata.Should().BeNull();
        error.Should().Be(DownloadError.NotFound);
    }

    [Fact]
    public async Task GetMetadataAsync_ExpiredFile_ReturnsExpired()
    {
        SetupFiles(File("tok", expiresAt: DateTime.UtcNow.AddDays(-1)));

        var (_, error) = await _service.GetMetadataAsync("tok");

        error.Should().Be(DownloadError.Expired);
    }

    [Fact]
    public async Task GetMetadataAsync_ValidFile_ReturnsMetadata()
    {
        SetupFiles(File("tok", passwordHash: "hash"));

        var (metadata, error) = await _service.GetMetadataAsync("tok");

        error.Should().BeNull();
        metadata!.IsProtected.Should().BeTrue();
        metadata.OriginalName.Should().Be("test.txt");
    }

    [Fact]
    public async Task DownloadAsync_UnknownToken_ReturnsFailureNotFound()
    {
        SetupFiles();

        var result = await _service.DownloadAsync("unknown", null);

        result.Should().BeOfType<DownloadResult.Failure>()
              .Which.Error.Should().Be(DownloadError.NotFound);
    }

    [Fact]
    public async Task DownloadAsync_ExpiredFile_ReturnsFailureExpired()
    {
        SetupFiles(File("tok", expiresAt: DateTime.UtcNow.AddHours(-1)));

        var result = await _service.DownloadAsync("tok", null);

        result.Should().BeOfType<DownloadResult.Failure>()
              .Which.Error.Should().Be(DownloadError.Expired);
    }

    [Fact]
    public async Task DownloadAsync_ProtectedFileWithoutPassword_ReturnsFailurePasswordRequired()
    {
        SetupFiles(File("tok", passwordHash: "hash"));

        var result = await _service.DownloadAsync("tok", null);

        result.Should().BeOfType<DownloadResult.Failure>()
              .Which.Error.Should().Be(DownloadError.PasswordRequired);
    }

    [Fact]
    public async Task DownloadAsync_ProtectedFileWithWrongPassword_ReturnsFailureInvalidPassword()
    {
        SetupFiles(File("tok", passwordHash: "hash"));
        _hasher.Verify("hash", "wrong").Returns(false);

        var result = await _service.DownloadAsync("tok", "wrong");

        result.Should().BeOfType<DownloadResult.Failure>()
              .Which.Error.Should().Be(DownloadError.InvalidPassword);
    }

    [Fact]
    public async Task DownloadAsync_ProtectedFileWithCorrectPassword_ReturnsSuccess()
    {
        SetupFiles(File("tok", passwordHash: "hash"));
        _hasher.Verify("hash", "good").Returns(true);
        _storage.OpenReadAsync("path.bin", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Stream>(new MemoryStream()));

        var result = await _service.DownloadAsync("tok", "good");

        result.Should().BeOfType<DownloadResult.Success>();
        var success = (DownloadResult.Success)result;
        success.FileName.Should().Be("test.txt");
        success.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task DownloadAsync_UnprotectedFile_ReturnsSuccess()
    {
        SetupFiles(File("tok"));
        _storage.OpenReadAsync("path.bin", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Stream>(new MemoryStream()));

        var result = await _service.DownloadAsync("tok", null);

        result.Should().BeOfType<DownloadResult.Success>();
    }
}
