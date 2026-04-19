using DataShare.Application.Interfaces;
using DataShare.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Infrastructure.Data;

public class ApplicationDbContext
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<FileTag> FileTags => Set<FileTag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StoredFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.MimeType).HasMaxLength(255).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.DownloadToken).HasMaxLength(64).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasIndex(e => e.DownloadToken).IsUnique();
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.ExpiresAt);
            // Index composite pour la requête de hard-delete (phase 2 purge)
            entity.HasIndex(e => new { e.IsPurged, e.ExpiresAt });

            entity.HasOne<IdentityUser<Guid>>()
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(30).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasIndex(e => new { e.OwnerId, e.Name }).IsUnique();
            entity.HasIndex(e => e.OwnerId);

            entity.HasOne<IdentityUser<Guid>>()
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FileTag>(entity =>
        {
            entity.HasKey(e => new { e.FileId, e.TagId });

            entity.HasOne(e => e.File)
                  .WithMany(f => f.FileTags)
                  .HasForeignKey(e => e.FileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                  .WithMany(t => t.FileTags)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
