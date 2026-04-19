using System.Text;
using DataShare.Application.Interfaces;
using DataShare.Application.Services;
using DataShare.Domain.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace DataShare.Tests.Unit.Services;

/// <summary>
/// Test de non-régression pour la race condition sur la création de tags concurrente.
/// Cf. commit fix: corrections après review humaine sur US05.
/// </summary>
public class FileUploadServiceTagRaceTests
{
    [Fact]
    public async Task GetOrCreateTagAsync_WhenFirstSaveFailsWithDbUpdateException_DetachesNewTagAndRetries()
    {
        // Scénario : un autre upload concurrent vient de créer le tag "demo".
        // Le 1er SaveChanges lève DbUpdateException (violation contrainte unique).
        // Le service doit détacher le newTag pour éviter une nouvelle insertion,
        // puis re-fetch et trouver le tag existant.

        var ownerId = Guid.NewGuid();
        var existingTag = new Tag
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "demo",
            CreatedAt = DateTime.UtcNow
        };

        // Premier fetch : vide (le tag n'existe pas encore côté notre contexte).
        // Après catch DbUpdateException : second fetch doit retourner le tag existant.
        // On simule cela avec une liste mutable.
        var tagList = new List<Tag>();

        var db = Substitute.For<IApplicationDbContext>();
        var storedFilesSet = new List<StoredFile>().BuildMockDbSet();
        var tagsSet = tagList.BuildMockDbSet();
        var fileTagsSet = new List<FileTag>().BuildMockDbSet();
        db.StoredFiles.Returns(storedFilesSet);
        db.Tags.Returns(tagsSet);
        db.FileTags.Returns(fileTagsSet);

        // Compte les appels à SaveChangesAsync : 1er = race (DbUpdateException), 2e = insertion
        // du StoredFile après récupération du tag existant (réussit).
        var saveChangesCallCount = 0;
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            saveChangesCallCount++;
            if (saveChangesCallCount == 1)
            {
                // Simule la race : le tag existe maintenant côté base externe, insertion refusée.
                // On l'ajoute côté notre list pour que le retry le trouve.
                tagList.Add(existingTag);
                var refreshedSet = tagList.BuildMockDbSet();
                db.Tags.Returns(refreshedSet);
                throw new DbUpdateException("unique violation on (OwnerId, Name)");
            }
            return Task.FromResult(1);
        });

        var storage = Substitute.For<IFileStorageService>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult("2026/04/file.bin"));

        var hasher = Substitute.For<IFilePasswordHasher>();
        var service = new FileUploadService(db, storage, hasher);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        var (result, error, _) = await service.UploadAsync(
            stream, "doc.txt", "text/plain", 7, ownerId, 1, null, new[] { "demo" }, "http://host");

        error.Should().BeNull();
        result.Should().NotBeNull();
        result!.Tags.Should().ContainSingle(t => t == "demo");
        // Le 2e SaveChanges doit avoir été appelé pour persister le StoredFile + FileTag
        saveChangesCallCount.Should().BeGreaterThanOrEqualTo(2);
    }
}
