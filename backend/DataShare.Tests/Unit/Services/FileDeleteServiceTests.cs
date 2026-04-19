using DataShare.Application.Interfaces;
using DataShare.Application.Services;
using DataShare.Domain.Entities;
using AwesomeAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace DataShare.Tests.Unit.Services;

public class FileDeleteServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly FileDeleteService _service;

    public FileDeleteServiceTests()
    {
        _db = Substitute.For<IApplicationDbContext>();
        _storage = Substitute.For<IFileStorageService>();
        _service = new FileDeleteService(_db, _storage);
    }

    private StoredFile CreateFile(Guid? ownerId, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        OwnerId = ownerId,
        OriginalName = "f.txt",
        MimeType = "text/plain",
        SizeBytes = 10,
        StoragePath = "2026/04/blob.bin",
        DownloadToken = "tok",
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task DeleteAsync_FileDoesNotExist_ReturnsFalse()
    {
        var set = new List<StoredFile>().BuildMockDbSet();
        _db.StoredFiles.Returns(set);

        var deleted = await _service.DeleteAsync(_userId, Guid.NewGuid());

        deleted.Should().BeFalse();
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_FileOwnedByOtherUser_ReturnsFalseIndistinguishableFromNotFound()
    {
        var fileId = Guid.NewGuid();
        var set = new List<StoredFile> { CreateFile(_otherUserId, fileId) }.BuildMockDbSet();
        _db.StoredFiles.Returns(set);

        var deleted = await _service.DeleteAsync(_userId, fileId);

        deleted.Should().BeFalse();
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_OwnedFile_DeletesBlobAndReturnsTrue()
    {
        var fileId = Guid.NewGuid();
        var file = CreateFile(_userId, fileId);
        var set = new List<StoredFile> { file }.BuildMockDbSet();
        _db.StoredFiles.Returns(set);

        var deleted = await _service.DeleteAsync(_userId, fileId);

        deleted.Should().BeTrue();
        await _storage.Received(1).DeleteAsync("2026/04/blob.bin", Arg.Any<CancellationToken>());
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
