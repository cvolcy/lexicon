using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lexicon.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChunkVectors",
                columns: table => new
                {
                    ChunkId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimensions = table.Column<int>(type: "INTEGER", nullable: false),
                    Embedding = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunkVectors", x => x.ChunkId);
                    table.ForeignKey(
                        name: "FK_ChunkVectors_DocumentChunks_ChunkId",
                        column: x => x.ChunkId,
                        principalTable: "DocumentChunks",
                        principalColumn: "ChunkId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChunkVectors_DocumentId",
                table: "ChunkVectors",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChunkVectors_Model",
                table: "ChunkVectors",
                column: "Model");

            migrationBuilder.Sql("""
                CREATE VIRTUAL TABLE IF NOT EXISTS "ChunksFts" USING fts5(
                    "ChunkId" UNINDEXED,
                    "DocumentId" UNINDEXED,
                    "Text",
                    tokenize = 'unicode61 remove_diacritics 2'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "ChunksFts";""");

            migrationBuilder.DropTable(
                name: "ChunkVectors");
        }
    }
}
