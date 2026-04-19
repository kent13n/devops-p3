using DataShare.Application.Interfaces;
using DataShare.Application.Services;
using DataShare.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace DataShare.Tests.Unit.Services;

public class FileListServiceTests
{
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private (FileListService service, IApplicationDbContext db) BuildService(IEnumerable<StoredFile> files)
    {
        var fileList = files.ToList();
        var mockDbSet = fileList.BuildMockDbSet();
        var db = Substitute.For<IApplicationDbContext>();
        db.StoredFiles.Returns(mockDbSet);
        return (new FileListService(db), db);
    }

    private StoredFile CreateFile(Guid? ownerId = null, DateTime? expiresAt = null, bool isPurged = false, bool isProtected = false)
    {
        return new StoredFile
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            OriginalName = "test.txt",
            MimeType = "text/plain",
            SizeBytes = 100,
            StoragePath = "path",
            DownloadToken = Guid.NewGuid().ToString("N"),
            PasswordHash = isProtected ? "hashed" : null,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1),
            IsPurged = isPurged,
            CreatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60))
        };
    }

    [Fact]
    public async Task GetUserFilesAsync_All_ReturnsAllUserFiles()
    {
        var files = new[]
        {
            CreateFile(_userId, DateTime.UtcNow.AddDays(1)),
            CreateFile(_userId, DateTime.UtcNow.AddDays(-1)),
            CreateFile(_otherUserId, DateTime.UtcNow.AddDays(1))
        };
        var (service, _) = BuildService(files);

        var result = await service.GetUserFilesAsync(_userId, FileStatusFilter.All, "http://host");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(f => files.Select(sf => sf.Id).Contains(f.Id));
    }

    [Fact]
    public async Task GetUserFilesAsync_Active_FiltersExpiresAtFuture()
    {
        var files = new[]
        {
            CreateFile(_userId, DateTime.UtcNow.AddDays(1)),
            CreateFile(_userId, DateTime.UtcNow.AddDays(-1))
        };
        var (service, _) = BuildService(files);

        var result = await service.GetUserFilesAsync(_userId, FileStatusFilter.Active, "http://host");

        result.Should().HaveCount(1);
        result[0].IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserFilesAsync_Expired_FiltersExpiresAtPast()
    {
        var files = new[]
        {
            CreateFile(_userId, DateTime.UtcNow.AddDays(1)),
            CreateFile(_userId, DateTime.UtcNow.AddDays(-1))
        };
        var (service, _) = BuildService(files);

        var result = await service.GetUserFilesAsync(_userId, FileStatusFilter.Expired, "http://host");

        result.Should().HaveCount(1);
        result[0].IsExpired.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserFilesAsync_ExpiredFile_HasNullDownloadUrl()
    {
        var files = new[] { CreateFile(_userId, DateTime.UtcNow.AddDays(-1)) };
        var (service, _) = BuildService(files);

        var result = await service.GetUserFilesAsync(_userId, FileStatusFilter.All, "http://host");

        result[0].DownloadUrl.Should().BeNull();
        result[0].IsExpired.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserFilesAsync_PurgedFile_HasNullDownloadUrl()
    {
        var files = new[] { CreateFile(_userId, DateTime.UtcNow.AddDays(1), isPurged: true) };
        var (service, _) = BuildService(files);

        var result = await service.GetUserFilesAsync(_userId, FileStatusFilter.All, "http://host");

        result[0].DownloadUrl.Should().BeNull();
        result[0].IsPurged.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserFilesAsync_ActiveFile_HasDownloadUrlWithBaseUrlAndToken()
    {
        var file = CreateFile(_userId, DateTime.UtcNow.AddDays(1));
        var (service, _) = BuildService(new[] { file });

        var result = await service.GetUserFilesAsync(_userId, FileStatusFilter.All, "http://host/");

        result[0].DownloadUrl.Should().Be($"http://host/d/{file.DownloadToken}");
    }
}
