using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lexicon.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.DocumentId);
        builder.Property(d => d.DocumentId).ValueGeneratedNever();

        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(255).IsRequired();
        builder.Property(d => d.SizeBytes).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(512).IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(d => d.Error).HasMaxLength(2000);
        builder.Property(d => d.ChunkCount).HasDefaultValue(0).IsRequired();

        // Stored as a sortable integer so ORDER BY / comparisons work on every provider (SQLite included).
        builder.Property(d => d.CreatedAt)
            .HasConversion<DateTimeOffsetToBinaryConverter>()
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasConversion<DateTimeOffsetToBinaryConverter>()
            .IsRequired();

        builder.HasIndex(d => d.CreatedAt);
        builder.HasIndex(d => d.Status);
    }
}
