using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AVA.Memory.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemoryRecords",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", maxLength: 4000, nullable: true),
                    EpisodeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ContextId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Salience = table.Column<double>(type: "float", nullable: false),
                    Novelty = table.Column<double>(type: "float", nullable: false),
                    Frequency = table.Column<double>(type: "float", nullable: false),
                    DecayRate = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryRecords", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AssociationEdges",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FromRecordID = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    ToRecordID = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationEdges", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AssociationEdges_MemoryRecords_FromRecordID",
                        column: x => x.FromRecordID,
                        principalTable: "MemoryRecords",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssociationEdges_MemoryRecords_ToRecordID",
                        column: x => x.ToRecordID,
                        principalTable: "MemoryRecords",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemoryMetadata",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordID = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryMetadata", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MemoryMetadata_MemoryRecords_RecordID",
                        column: x => x.RecordID,
                        principalTable: "MemoryRecords",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryTags",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordID = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryTags", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MemoryTags_MemoryRecords_RecordID",
                        column: x => x.RecordID,
                        principalTable: "MemoryRecords",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryVectors",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordID = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryVectors", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MemoryVectors_MemoryRecords_RecordID",
                        column: x => x.RecordID,
                        principalTable: "MemoryRecords",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationEdges_FromRecordID",
                table: "AssociationEdges",
                column: "FromRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationEdges_ToRecordID",
                table: "AssociationEdges",
                column: "ToRecordID");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryMetadata_RecordID_Key",
                table: "MemoryMetadata",
                columns: new[] { "RecordID", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRecords_CreatedAt",
                table: "MemoryRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRecords_LastAccessedAt",
                table: "MemoryRecords",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRecords_Salience",
                table: "MemoryRecords",
                column: "Salience");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryRecords_UpdatedAt",
                table: "MemoryRecords",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryTags_RecordID_Tag",
                table: "MemoryTags",
                columns: new[] { "RecordID", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryVectors_RecordID_Index",
                table: "MemoryVectors",
                columns: new[] { "RecordID", "Index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssociationEdges");

            migrationBuilder.DropTable(
                name: "MemoryMetadata");

            migrationBuilder.DropTable(
                name: "MemoryTags");

            migrationBuilder.DropTable(
                name: "MemoryVectors");

            migrationBuilder.DropTable(
                name: "MemoryRecords");
        }
    }
}
