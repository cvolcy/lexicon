using Lexicon.Models.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lexicon.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.ConversationId);
        builder.Property(c => c.ConversationId).ValueGeneratedNever();

        builder.Property(c => c.Title).HasMaxLength(Conversation.MaxTitleLength).IsRequired();

        // Stored as a sortable integer so ORDER BY / comparisons work on every provider (SQLite included).
        builder.Property(c => c.CreatedAt)
            .HasConversion<DateTimeOffsetToBinaryConverter>()
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasConversion<DateTimeOffsetToBinaryConverter>()
            .IsRequired();

        builder.HasIndex(c => c.UpdatedAt);
    }
}
