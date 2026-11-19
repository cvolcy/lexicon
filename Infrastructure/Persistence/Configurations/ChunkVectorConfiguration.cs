using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexicon.Infrastructure.Persistence.Configurations;

public class ChunkVectorConfiguration : IEntityTypeConfiguration<ChunkVector>
{
    public void Configure(EntityTypeBuilder<ChunkVector> builder)
    {
        builder.ToTable("ChunkVectors");

        builder.HasKey(v => v.ChunkId);
        builder.Property(v => v.ChunkId).ValueGeneratedNever();

        builder.Property(v => v.DocumentId).IsRequired();
        builder.Property(v => v.Dimensions).IsRequired();
        builder.Property(v => v.Embedding).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(255).IsRequired();

        builder.HasOne(v => v.Chunk)
            .WithOne(c => c.Vector)
            .HasForeignKey<ChunkVector>(v => v.ChunkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.DocumentId);
        builder.HasIndex(v => v.Model);
    }
}
