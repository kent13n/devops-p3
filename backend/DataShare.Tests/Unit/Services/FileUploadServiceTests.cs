using System.Text;
using DataShare.Application.Interfaces;
using DataShare.Application.Services;
using DataShare.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace DataShare.Tests.Unit.Services;

public class FileUploadServiceTests
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IFilePasswordHasher _hasher;
    private readonly FileUploadService _service;

    public FileUploadServiceTests()
    {
        _db = Substitute.For<IApplicationDbContext>();
        var storedFilesSet = new List<StoredFile>().BuildMockDbSet();
        var tagsSet = new List<Tag>().BuildMockDbSet();
        var fileTagsSet = new List<FileTag>().BuildMockDbSet();
        _db.StoredFiles.Returns(storedFilesSet);
        _db.Tags.Returns(tagsSet);
        _db.FileTags.Returns(fileTagsSet);

        _storage = Substitute.For<IFileStorageService>();
        _storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("2026/04/test.bin"));

        _hasher = Substitute.For<IFilePasswordHasher>();
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        _service = new FileUploadService(_db, _storage, _hasher);
    }

    private static Stream EmptyStream() => new MemoryStream(Encoding.UTF8.GetBytes("hello"));

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("script.sh")]
    [InlineData("malware.bat")]
    [InlineData("trojan.svg")]
    public async Task UploadAsync_BlockedExtension_ReturnsError(string fileName)
    {
        var (result, error, detail) = await _service.UploadAsync(
            EmptyStream(), fileName, "text/plain", 10, null, null, null, null, "http://host");

        result.Should().BeNull();
        error.Should().Be(UploadError.BlockedExtension);
        detail.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UploadAsync_FileTooLarge_ReturnsError()
    {
        var (result, error, _) = await _service.UploadAsync(
            EmptyStream(), "big.txt", "text/plain", 2_000_000_000, null, null, null, null, "http://host");

        result.Should().BeNull();
        error.Should().Be(UploadError.FileTooLarge);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public async Task UploadAsync_InvalidExpiration_ReturnsError(int days)
    {
        var (result, error, _) = await _service.UploadAsync(
            EmptyStream(), "f.txt", "text/plain", 10, null, days, null, null, "http://host");

        result.Should().BeNull();
        error.Should().Be(UploadError.InvalidExpiration);
    }

    [Fact]
    public async Task UploadAsync_PasswordTooShort_ReturnsError()
    {
        var (result, error, _) = await _service.UploadAsync(
            EmptyStream(), "f.txt", "text/plain", 10, null, 1, "abc", null, "http://host");

        result.Should().BeNull();
        error.Should().Be(UploadError.PasswordTooShort);
    }

    [Fact]
    public async Task UploadAsync_EmptyPassword_IsNormalizedToNull()
    {
        // "" doit être traité comme pas de password, pas comme password trop court
        var (result, error, _) = await _service.UploadAsync(
            EmptyStream(), "f.txt", "text/plain", 5, null, 1, "", null, "http://host");

        error.Should().BeNull();
        result.Should().NotBeNull();
        result!.IsProtected.Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_AnonymousUser_IgnoresTags()
    {
        var tags = new[] { "projet", "important" };

        var (result, error, _) = await _service.UploadAsync(
            EmptyStream(), "f.txt", "text/plain", 5, null, 1, null, tags, "http://host");

        error.Should().BeNull();
        result!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_Success_GeneratesDownloadTokenBase64UrlOf43Chars()
    {
        var (result, _, _) = await _service.UploadAsync(
            EmptyStream(), "f.txt", "text/plain", 5, null, 1, null, null, "http://host");

        result!.DownloadToken.Should().HaveLength(43);
        result.DownloadToken.Should().MatchRegex("^[A-Za-z0-9_-]+$"); // base64url
    }

    [Fact]
    public async Task UploadAsync_DbFailure_DeletesBlob()
    {
        _db.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
           .Do(_ => throw new InvalidOperationException("DB failure"));

        var act = () => _service.UploadAsync(
            EmptyStream(), "f.txt", "text/plain", 5, null, 1, null, null, "http://host");

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _storage.Received(1).DeleteAsync("2026/04/test.bin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WithPassword_HashesAndMarksProtected()
    {
        var (result, _, _) = await _service.UploadAsync(
            EmptyStream(), "f.txt", "text/plain", 5, null, 1, "secret123", null, "http://host");

        result!.IsProtected.Should().BeTrue();
        _hasher.Received(1).Hash("secret123");
    }
}
