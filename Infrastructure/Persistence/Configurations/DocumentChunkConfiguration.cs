using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexicon.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");

        builder.HasKey(c => c.ChunkId);
        builder.Property(c => c.ChunkId).ValueGeneratedNever();

        builder.Property(c => c.Index).IsRequired();
        builder.Property(c => c.Text).IsRequired();
        builder.Property(c => c.Section).HasMaxLength(512);
        builder.Property(c => c.TokenCount).IsRequired();

        builder.HasOne(c => c.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.DocumentId, c.Index }).IsUnique();
    }
}
