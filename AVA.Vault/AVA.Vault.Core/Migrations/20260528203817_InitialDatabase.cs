using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AVA.Vault.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AvaProviderProfiles",
                columns: table => new
                {
                    ProviderProfileId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ApiKeySecretRef = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    BaseUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    CustomProviderType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CustomTransportType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CustomHeadersAsText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransportType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    SecondarySecretRef = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SupportsStreaming = table.Column<bool>(type: "bit", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaProviderProfiles", x => x.ProviderProfileId);
                });

            migrationBuilder.CreateTable(
                name: "AvaSecrets",
                columns: table => new
                {
                    SecretId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SecretRef = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SecretName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SecretType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EncryptedValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptionProvider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaSecrets", x => x.SecretId);
                });

            migrationBuilder.CreateTable(
                name: "ModuleIdentity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleAvaId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleIdentity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaultHeaders",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultHeaders", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AvaModelDefinitions",
                columns: table => new
                {
                    ModelDefinitionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ApiKeyOverrideRef = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ContextWindowTokens = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    DefaultTemperature = table.Column<double>(type: "float", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EndpointOverride = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsDiscovered = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxInputCharacters = table.Column<int>(type: "int", nullable: true),
                    MaxOutputTokens = table.Column<int>(type: "int", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModelType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SupportsProviderMemory = table.Column<bool>(type: "bit", nullable: false),
                    SupportsReasoning = table.Column<bool>(type: "bit", nullable: false),
                    SupportsStreaming = table.Column<bool>(type: "bit", nullable: false),
                    SupportsTools = table.Column<bool>(type: "bit", nullable: false),
                    SupportsVision = table.Column<bool>(type: "bit", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    ProviderProfileId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaModelDefinitions", x => x.ModelDefinitionId);
                    table.ForeignKey(
                        name: "FK_AvaModelDefinitions_AvaProviderProfiles_ProviderProfileId",
                        column: x => x.ProviderProfileId,
                        principalTable: "AvaProviderProfiles",
                        principalColumn: "ProviderProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultProjects",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsExpanded = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    VaultID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultProjects", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultProjects_VaultHeaders_VaultID",
                        column: x => x.VaultID,
                        principalTable: "VaultHeaders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultGraphs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    GraphData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultGraphs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultGraphs_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultSessions",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    AttachedModelIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BroadcastGroupIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanvasJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultModelId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SpawnCount = table.Column<int>(type: "int", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    VaultID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultSessions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultSessions_VaultHeaders_VaultID",
                        column: x => x.VaultID,
                        principalTable: "VaultHeaders",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultSessions_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "VaultTags",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultTags", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultTags_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflows",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflows", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflows_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileOrder = table.Column<int>(type: "int", nullable: false),
                    VaultID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SessionID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultFileRefs_VaultHeaders_VaultID",
                        column: x => x.VaultID,
                        principalTable: "VaultHeaders",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultFileRefs_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultFileRefs_VaultSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "VaultSessions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "VaultNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    VaultID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SessionID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultNotes_VaultHeaders_VaultID",
                        column: x => x.VaultID,
                        principalTable: "VaultHeaders",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultNotes_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultNotes_VaultSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "VaultSessions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowNodes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NodeOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    WorkflowID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowNodes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowNodes_VaultWorkflows_WorkflowID",
                        column: x => x.WorkflowID,
                        principalTable: "VaultWorkflows",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultFileRefRelations",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    SourceFileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetFileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultFileRefRelations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultFileRefRelations_VaultFileRefs_SourceFileRefID",
                        column: x => x.SourceFileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultFileRefRelations_VaultFileRefs_TargetFileRefID",
                        column: x => x.TargetFileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "VaultHeaderFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VaultID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultHeaderFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultHeaderFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultHeaderFileRefs_VaultHeaders_VaultID",
                        column: x => x.VaultID,
                        principalTable: "VaultHeaders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultProjectFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultProjectFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultProjectFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultProjectFileRefs_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultSessionFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SessionID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultSessionFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultSessionFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultSessionFileRefs_VaultSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "VaultSessions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowFileRefs_VaultWorkflows_WorkflowID",
                        column: x => x.WorkflowID,
                        principalTable: "VaultWorkflows",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultFileRefNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    NoteOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultFileRefNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultFileRefNotes_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultFileRefNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "VaultHeaderNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VaultID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultHeaderNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultHeaderNotes_VaultHeaders_VaultID",
                        column: x => x.VaultID,
                        principalTable: "VaultHeaders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultHeaderNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "VaultMetadata",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OwnerID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultMetadata", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultMetadata_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultNoteFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultNoteFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultNoteFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultNoteFileRefs_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultNoteRelations",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    SourceNoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetNoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultNoteRelations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultNoteRelations_VaultNotes_SourceNoteID",
                        column: x => x.SourceNoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultNoteRelations_VaultNotes_TargetNoteID",
                        column: x => x.TargetNoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "VaultNoteVaultTags",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TagID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultNoteVaultTags", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultNoteVaultTags_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultNoteVaultTags_VaultTags_TagID",
                        column: x => x.TagID,
                        principalTable: "VaultTags",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultProjectNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProjectID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultProjectNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultProjectNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultProjectNotes_VaultProjects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "VaultProjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultSessionNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SessionID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultSessionNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultSessionNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultSessionNotes_VaultSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "VaultSessions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowNotes_VaultWorkflows_WorkflowID",
                        column: x => x.WorkflowID,
                        principalTable: "VaultWorkflows",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowLines",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConditionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefaultLine = table.Column<bool>(type: "bit", nullable: false),
                    LineType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LineOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    SourceWorkflowNodeID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetWorkflowNodeID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowLines", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLines_VaultWorkflowNodes_SourceWorkflowNodeID",
                        column: x => x.SourceWorkflowNodeID,
                        principalTable: "VaultWorkflowNodes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLines_VaultWorkflowNodes_TargetWorkflowNodeID",
                        column: x => x.TargetWorkflowNodeID,
                        principalTable: "VaultWorkflowNodes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLines_VaultWorkflows_WorkflowID",
                        column: x => x.WorkflowID,
                        principalTable: "VaultWorkflows",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowNodeFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowNodeID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowNodeFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowNodeFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowNodeFileRefs_VaultWorkflowNodes_WorkflowNodeID",
                        column: x => x.WorkflowNodeID,
                        principalTable: "VaultWorkflowNodes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowNodeNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    NoteOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowNodeID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowNodeNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowNodeNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowNodeNotes_VaultWorkflowNodes_WorkflowNodeID",
                        column: x => x.WorkflowNodeID,
                        principalTable: "VaultWorkflowNodes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowLineFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    FileOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowLineID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowLineFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineFileRefs_VaultWorkflowLines_WorkflowLineID",
                        column: x => x.WorkflowLineID,
                        principalTable: "VaultWorkflowLines",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowLineNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowLineID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowLineNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineNotes_VaultWorkflowLines_WorkflowLineID",
                        column: x => x.WorkflowLineID,
                        principalTable: "VaultWorkflowLines",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowLineSteps",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    WorkflowLineID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowLineSteps", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineSteps_VaultWorkflowLines_WorkflowLineID",
                        column: x => x.WorkflowLineID,
                        principalTable: "VaultWorkflowLines",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowLineStepFileRefs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileRefID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowLineStepID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowLineStepFileRefs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineStepFileRefs_VaultFileRefs_FileRefID",
                        column: x => x.FileRefID,
                        principalTable: "VaultFileRefs",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineStepFileRefs_VaultWorkflowLineSteps_WorkflowLineStepID",
                        column: x => x.WorkflowLineStepID,
                        principalTable: "VaultWorkflowLineSteps",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultWorkflowLineStepNotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoteID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowLineStepID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrimaryIdentityHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryIdentityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdentityList = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultWorkflowLineStepNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineStepNotes_VaultNotes_NoteID",
                        column: x => x.NoteID,
                        principalTable: "VaultNotes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_VaultWorkflowLineStepNotes_VaultWorkflowLineSteps_WorkflowLineStepID",
                        column: x => x.WorkflowLineStepID,
                        principalTable: "VaultWorkflowLineSteps",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvaModelDefinitions_IsActive",
                table: "AvaModelDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AvaModelDefinitions_IsDefault",
                table: "AvaModelDefinitions",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_AvaModelDefinitions_IsDiscovered",
                table: "AvaModelDefinitions",
                column: "IsDiscovered");

            migrationBuilder.CreateIndex(
                name: "IX_AvaModelDefinitions_ProviderProfileId",
                table: "AvaModelDefinitions",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaModelDefinitions_ProviderProfileId_SortOrder",
                table: "AvaModelDefinitions",
                columns: new[] { "ProviderProfileId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_AvaModelDefinitions_ProviderProfileId_ModelId",
                table: "AvaModelDefinitions",
                columns: new[] { "ProviderProfileId", "ModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvaProviderProfiles_IsActive",
                table: "AvaProviderProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AvaProviderProfiles_IsDefault",
                table: "AvaProviderProfiles",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_AvaProviderProfiles_Name",
                table: "AvaProviderProfiles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AvaProviderProfiles_ProviderType",
                table: "AvaProviderProfiles",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_AvaProviderProfiles_SortOrder",
                table: "AvaProviderProfiles",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_AvaProviderProfiles_TransportType",
                table: "AvaProviderProfiles",
                column: "TransportType");

            migrationBuilder.CreateIndex(
                name: "IX_AvaSecrets_IsActive",
                table: "AvaSecrets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AvaSecrets_SecretType",
                table: "AvaSecrets",
                column: "SecretType");

            migrationBuilder.CreateIndex(
                name: "UX_AvaSecrets_SecretRef",
                table: "AvaSecrets",
                column: "SecretRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefNotes_FileRefID",
                table: "VaultFileRefNotes",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefNotes_FileRefId_SortOrder",
                table: "VaultFileRefNotes",
                columns: new[] { "FileRefID", "NoteOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefNotes_NoteID",
                table: "VaultFileRefNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "UX_VaultFileRefNotes_FileRefId_NoteID",
                table: "VaultFileRefNotes",
                columns: new[] { "FileRefID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefRelations_SourceFileRefID",
                table: "VaultFileRefRelations",
                column: "SourceFileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefRelations_SourceFileRefId_SortOrder",
                table: "VaultFileRefRelations",
                columns: new[] { "SourceFileRefID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefRelations_TargetFileRefID",
                table: "VaultFileRefRelations",
                column: "TargetFileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefs_ProjectID",
                table: "VaultFileRefs",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefs_SessionID",
                table: "VaultFileRefs",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultFileRefs_VaultID",
                table: "VaultFileRefs",
                column: "VaultID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultGraphs_ProjectID",
                table: "VaultGraphs",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultHeaderFileRefs_FileRefID",
                table: "VaultHeaderFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultHeaderFileRefs_VaultID",
                table: "VaultHeaderFileRefs",
                column: "VaultID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultHeaderFileRefs_VaultId_SortOrder",
                table: "VaultHeaderFileRefs",
                columns: new[] { "VaultID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultHeaderFileRefs_VaultId_FileRefID",
                table: "VaultHeaderFileRefs",
                columns: new[] { "VaultID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultHeaderNotes_NoteID",
                table: "VaultHeaderNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultHeaderNotes_VaultID",
                table: "VaultHeaderNotes",
                column: "VaultID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultHeaderNotes_VaultId_SortOrder",
                table: "VaultHeaderNotes",
                columns: new[] { "VaultID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultHeaderNotes_VaultId_NoteID",
                table: "VaultHeaderNotes",
                columns: new[] { "VaultID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultMetadata_NoteID",
                table: "VaultMetadata",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultMetadata_NoteId_Key",
                table: "VaultMetadata",
                columns: new[] { "NoteID", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteFileRefs_FileRefID",
                table: "VaultNoteFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteFileRefs_NoteID",
                table: "VaultNoteFileRefs",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteFileRefs_NoteId_SortOrder",
                table: "VaultNoteFileRefs",
                columns: new[] { "NoteID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultNoteFileRefs_NoteId_FileRefID",
                table: "VaultNoteFileRefs",
                columns: new[] { "NoteID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteRelations_SourceNoteID",
                table: "VaultNoteRelations",
                column: "SourceNoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteRelations_SourceNoteId_SortOrder",
                table: "VaultNoteRelations",
                columns: new[] { "SourceNoteID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteRelations_TargetNoteID",
                table: "VaultNoteRelations",
                column: "TargetNoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNotes_ProjectID",
                table: "VaultNotes",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNotes_ProjectId_SortOrder",
                table: "VaultNotes",
                columns: new[] { "ProjectID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultNotes_SessionID",
                table: "VaultNotes",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNotes_VaultID",
                table: "VaultNotes",
                column: "VaultID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteVaultTags_NoteID",
                table: "VaultNoteVaultTags",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultNoteVaultTags_TagID",
                table: "VaultNoteVaultTags",
                column: "TagID");

            migrationBuilder.CreateIndex(
                name: "UX_VaultNoteVaultTags_NoteID_TagID",
                table: "VaultNoteVaultTags",
                columns: new[] { "NoteID", "TagID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultProjectFileRefs_FileRefID",
                table: "VaultProjectFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultProjectFileRefs_ProjectID",
                table: "VaultProjectFileRefs",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultProjectFileRefs_ProjectId_SortOrder",
                table: "VaultProjectFileRefs",
                columns: new[] { "ProjectID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultProjectFileRefs_ProjectId_FileRefID",
                table: "VaultProjectFileRefs",
                columns: new[] { "ProjectID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultProjectNotes_NoteID",
                table: "VaultProjectNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultProjectNotes_ProjectID",
                table: "VaultProjectNotes",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultProjectNotes_ProjectId_SortOrder",
                table: "VaultProjectNotes",
                columns: new[] { "ProjectID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultProjectNotes_ProjectId_NoteID",
                table: "VaultProjectNotes",
                columns: new[] { "ProjectID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultProjects_VaultID",
                table: "VaultProjects",
                column: "VaultID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessionFileRefs_FileRefID",
                table: "VaultSessionFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessionFileRefs_SessionID",
                table: "VaultSessionFileRefs",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessionFileRefs_SessionId_SortOrder",
                table: "VaultSessionFileRefs",
                columns: new[] { "SessionID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultSessionFileRefs_SessionId_FileRefID",
                table: "VaultSessionFileRefs",
                columns: new[] { "SessionID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessionNotes_NoteID",
                table: "VaultSessionNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessionNotes_SessionID",
                table: "VaultSessionNotes",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessionNotes_SessionId_SortOrder",
                table: "VaultSessionNotes",
                columns: new[] { "SessionID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultSessionNotes_SessionId_NoteID",
                table: "VaultSessionNotes",
                columns: new[] { "SessionID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessions_ProjectID",
                table: "VaultSessions",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessions_ProjectId_SortOrder",
                table: "VaultSessions",
                columns: new[] { "ProjectID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultSessions_VaultID",
                table: "VaultSessions",
                column: "VaultID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultTags_ProjectID",
                table: "VaultTags",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "UX_VaultTags_ProjectId_Name",
                table: "VaultTags",
                columns: new[] { "ProjectID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowFileRefs_FileRefID",
                table: "VaultWorkflowFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowFileRefs_WorkflowID",
                table: "VaultWorkflowFileRefs",
                column: "WorkflowID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowFileRefs_WorkflowId_SortOrder",
                table: "VaultWorkflowFileRefs",
                columns: new[] { "WorkflowID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowFileRefs_WorkflowId_FileRefID",
                table: "VaultWorkflowFileRefs",
                columns: new[] { "WorkflowID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineFileRefs_FileRefID",
                table: "VaultWorkflowLineFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineFileRefs_WorkflowLineID",
                table: "VaultWorkflowLineFileRefs",
                column: "WorkflowLineID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineFileRefs_WorkflowLineId_SortOrder",
                table: "VaultWorkflowLineFileRefs",
                columns: new[] { "WorkflowLineID", "FileOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowLineFileRefs_WorkflowLineId_FileRefID",
                table: "VaultWorkflowLineFileRefs",
                columns: new[] { "WorkflowLineID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineNotes_NoteID",
                table: "VaultWorkflowLineNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineNotes_WorkflowLineID",
                table: "VaultWorkflowLineNotes",
                column: "WorkflowLineID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineNotes_WorkflowLineId_SortOrder",
                table: "VaultWorkflowLineNotes",
                columns: new[] { "WorkflowLineID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowLineNotes_WorkflowLineId_NoteID",
                table: "VaultWorkflowLineNotes",
                columns: new[] { "WorkflowLineID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLines_SourceWorkflowNodeID",
                table: "VaultWorkflowLines",
                column: "SourceWorkflowNodeID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLines_SourceWorkflowNodeId_SortOrder",
                table: "VaultWorkflowLines",
                columns: new[] { "SourceWorkflowNodeID", "LineOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLines_TargetWorkflowNodeID",
                table: "VaultWorkflowLines",
                column: "TargetWorkflowNodeID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLines_WorkflowID",
                table: "VaultWorkflowLines",
                column: "WorkflowID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineStepFileRefs_FileRefID",
                table: "VaultWorkflowLineStepFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineStepFileRefs_WorkflowLineStepID",
                table: "VaultWorkflowLineStepFileRefs",
                column: "WorkflowLineStepID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineStepFileRefs_WorkflowLineStepId_SortOrder",
                table: "VaultWorkflowLineStepFileRefs",
                columns: new[] { "WorkflowLineStepID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowLineStepFileRefs_WorkflowLineStepId_FileRefID",
                table: "VaultWorkflowLineStepFileRefs",
                columns: new[] { "WorkflowLineStepID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineStepNotes_NoteID",
                table: "VaultWorkflowLineStepNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineStepNotes_WorkflowLineStepID",
                table: "VaultWorkflowLineStepNotes",
                column: "WorkflowLineStepID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineStepNotes_WorkflowLineStepId_SortOrder",
                table: "VaultWorkflowLineStepNotes",
                columns: new[] { "WorkflowLineStepID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowLineStepNotes_WorkflowLineStepId_NoteID",
                table: "VaultWorkflowLineStepNotes",
                columns: new[] { "WorkflowLineStepID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowLineSteps_WorkflowLineID",
                table: "VaultWorkflowLineSteps",
                column: "WorkflowLineID");

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowLineSteps_WorkflowLineId_StepOrder",
                table: "VaultWorkflowLineSteps",
                columns: new[] { "WorkflowLineID", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodeFileRefs_FileRefID",
                table: "VaultWorkflowNodeFileRefs",
                column: "FileRefID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodeFileRefs_WorkflowNodeID",
                table: "VaultWorkflowNodeFileRefs",
                column: "WorkflowNodeID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodeFileRefs_WorkflowNodeId_SortOrder",
                table: "VaultWorkflowNodeFileRefs",
                columns: new[] { "WorkflowNodeID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowNodeFileRefs_WorkflowNodeId_FileRefID",
                table: "VaultWorkflowNodeFileRefs",
                columns: new[] { "WorkflowNodeID", "FileRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodeNotes_NoteID",
                table: "VaultWorkflowNodeNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodeNotes_WorkflowNodeID",
                table: "VaultWorkflowNodeNotes",
                column: "WorkflowNodeID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodeNotes_WorkflowNodeId_SortOrder",
                table: "VaultWorkflowNodeNotes",
                columns: new[] { "WorkflowNodeID", "NoteOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowNodeNotes_WorkflowNodeId_NoteID",
                table: "VaultWorkflowNodeNotes",
                columns: new[] { "WorkflowNodeID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodes_WorkflowID",
                table: "VaultWorkflowNodes",
                column: "WorkflowID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNodes_WorkflowId_SortOrder",
                table: "VaultWorkflowNodes",
                columns: new[] { "WorkflowID", "NodeOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNotes_NoteID",
                table: "VaultWorkflowNotes",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNotes_WorkflowID",
                table: "VaultWorkflowNotes",
                column: "WorkflowID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflowNotes_WorkflowId_SortOrder",
                table: "VaultWorkflowNotes",
                columns: new[] { "WorkflowID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VaultWorkflowNotes_WorkflowId_NoteID",
                table: "VaultWorkflowNotes",
                columns: new[] { "WorkflowID", "NoteID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflows_ProjectID",
                table: "VaultWorkflows",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_VaultWorkflows_ProjectId_SortOrder",
                table: "VaultWorkflows",
                columns: new[] { "ProjectID", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLog");

            migrationBuilder.DropTable(
                name: "AvaModelDefinitions");

            migrationBuilder.DropTable(
                name: "AvaSecrets");

            migrationBuilder.DropTable(
                name: "ModuleIdentity");

            migrationBuilder.DropTable(
                name: "VaultFileRefNotes");

            migrationBuilder.DropTable(
                name: "VaultFileRefRelations");

            migrationBuilder.DropTable(
                name: "VaultGraphs");

            migrationBuilder.DropTable(
                name: "VaultHeaderFileRefs");

            migrationBuilder.DropTable(
                name: "VaultHeaderNotes");

            migrationBuilder.DropTable(
                name: "VaultMetadata");

            migrationBuilder.DropTable(
                name: "VaultNoteFileRefs");

            migrationBuilder.DropTable(
                name: "VaultNoteRelations");

            migrationBuilder.DropTable(
                name: "VaultNoteVaultTags");

            migrationBuilder.DropTable(
                name: "VaultProjectFileRefs");

            migrationBuilder.DropTable(
                name: "VaultProjectNotes");

            migrationBuilder.DropTable(
                name: "VaultSessionFileRefs");

            migrationBuilder.DropTable(
                name: "VaultSessionNotes");

            migrationBuilder.DropTable(
                name: "VaultWorkflowFileRefs");

            migrationBuilder.DropTable(
                name: "VaultWorkflowLineFileRefs");

            migrationBuilder.DropTable(
                name: "VaultWorkflowLineNotes");

            migrationBuilder.DropTable(
                name: "VaultWorkflowLineStepFileRefs");

            migrationBuilder.DropTable(
                name: "VaultWorkflowLineStepNotes");

            migrationBuilder.DropTable(
                name: "VaultWorkflowNodeFileRefs");

            migrationBuilder.DropTable(
                name: "VaultWorkflowNodeNotes");

            migrationBuilder.DropTable(
                name: "VaultWorkflowNotes");

            migrationBuilder.DropTable(
                name: "AvaProviderProfiles");

            migrationBuilder.DropTable(
                name: "VaultTags");

            migrationBuilder.DropTable(
                name: "VaultWorkflowLineSteps");

            migrationBuilder.DropTable(
                name: "VaultFileRefs");

            migrationBuilder.DropTable(
                name: "VaultNotes");

            migrationBuilder.DropTable(
                name: "VaultWorkflowLines");

            migrationBuilder.DropTable(
                name: "VaultSessions");

            migrationBuilder.DropTable(
                name: "VaultWorkflowNodes");

            migrationBuilder.DropTable(
                name: "VaultWorkflows");

            migrationBuilder.DropTable(
                name: "VaultProjects");

            migrationBuilder.DropTable(
                name: "VaultHeaders");
        }
    }
}
