using LargeFileUpload.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LargeFileUpload.Infrastructure.Persistence.Configurations;

public class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.ToTable("UploadSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.BlobName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ContainerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.UploadStrategy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ChunkSize)
            .IsRequired(false);

        builder.Property(x => x.TotalChunks)
            .IsRequired(false);

        builder.Property(x => x.UploadedChunks)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Indexes
        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_UploadSessions_UserId_CreatedAt");

        builder.HasIndex(x => x.BlobName)
            .IsUnique()
            .HasDatabaseName("IX_UploadSessions_BlobName");

        builder.HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_UploadSessions_Status_ExpiresAt");
    }
}
