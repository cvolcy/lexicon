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

        builder.Property(d => d.CreatedAt)
            .HasConversion<DateTimeOffsetToBinaryConverter>()
            .IsRequired();

        builder.HasIndex(d => d.CreatedAt);
    }
}
