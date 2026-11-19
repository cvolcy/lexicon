using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Infrastructure.Persistence;

public class LexiconDbContext(DbContextOptions<LexiconDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<ChunkVector> ChunkVectors => Set<ChunkVector>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LexiconDbContext).Assembly);
    }
}
