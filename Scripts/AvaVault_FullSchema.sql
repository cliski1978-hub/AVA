IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [ActivityLog] (
        [ID] int NOT NULL IDENTITY,
        [Date] datetime2 NOT NULL,
        [UserName] nvarchar(100) NOT NULL,
        [Level] int NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [TargetID] nvarchar(max) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ActivityLog] PRIMARY KEY ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [AvaProviderProfiles] (
        [ID] nvarchar(128) NOT NULL,
        [ApiKeySecretRef] nvarchar(512) NULL,
        [BaseUrl] nvarchar(512) NULL,
        [CreatedAt] datetime NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsEnabled] bit NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [ProviderType] nvarchar(64) NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        CONSTRAINT [PK_AvaProviderProfiles] PRIMARY KEY ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [ModuleIdentity] (
        [Id] int NOT NULL IDENTITY,
        [ModuleAvaId] nvarchar(128) NOT NULL,
        [ModuleName] nvarchar(128) NOT NULL,
        [RegisteredAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ModuleIdentity] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultHeaders] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL,
        [LastSyncedAt] datetime NULL,
        [OwnerId] nvarchar(128) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_VaultHeaders] PRIMARY KEY ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [AvaModelDefinitions] (
        [ID] nvarchar(128) NOT NULL,
        [ContextWindowTokens] int NULL,
        [CreatedAt] datetime NOT NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsEnabled] bit NOT NULL,
        [MaxOutputTokens] int NULL,
        [ModelId] nvarchar(256) NOT NULL,
        [ModelType] nvarchar(64) NOT NULL,
        [SortOrder] int NOT NULL,
        [SupportsStreaming] bit NOT NULL,
        [SupportsTools] bit NOT NULL,
        [SupportsVision] bit NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [ProviderProfileID] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_AvaModelDefinitions] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_AvaModelDefinitions_AvaProviderProfiles_ProviderProfileID] FOREIGN KEY ([ProviderProfileID]) REFERENCES [AvaProviderProfiles] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultProjects] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsArchived] bit NOT NULL,
        [IsExpanded] bit NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [SortOrder] int NOT NULL,
        [Status] nvarchar(64) NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [VaultID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultProjects] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultProjects_VaultHeaders_VaultID] FOREIGN KEY ([VaultID]) REFERENCES [VaultHeaders] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultGraphs] (
        [ID] nvarchar(128) NOT NULL,
        [GeneratedAt] datetime NOT NULL,
        [GraphData] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [ProjectID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultGraphs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultGraphs_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultSessions] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [AttachedModelIdsJson] nvarchar(max) NULL,
        [BroadcastGroupIdsJson] nvarchar(max) NULL,
        [CanvasJson] nvarchar(max) NULL,
        [DefaultModelId] nvarchar(128) NULL,
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [IsPinned] bit NOT NULL,
        [IsTemplate] bit NOT NULL,
        [LastActiveAt] datetime NULL,
        [Name] nvarchar(256) NOT NULL,
        [SortOrder] int NOT NULL,
        [SpawnCount] int NOT NULL,
        [TemplateName] nvarchar(256) NULL,
        [UpdatedAt] datetime NOT NULL,
        [ProjectID] nvarchar(128) NOT NULL,
        [VaultID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultSessions] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultSessions_VaultHeaders_VaultID] FOREIGN KEY ([VaultID]) REFERENCES [VaultHeaders] ([ID]),
        CONSTRAINT [FK_VaultSessions_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultTags] (
        [ID] nvarchar(128) NOT NULL,
        [Color] nvarchar(32) NULL,
        [CreatedAt] datetime NOT NULL,
        [IsArchived] bit NOT NULL,
        [Metadata] nvarchar(512) NULL,
        [Name] nvarchar(256) NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [ProjectID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultTags] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultTags_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflows] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [Name] nvarchar(256) NOT NULL,
        [SortOrder] int NOT NULL,
        [Status] nvarchar(64) NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [WorkflowType] nvarchar(64) NOT NULL,
        [ProjectID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflows] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflows_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [FileSizeBytes] bigint NULL,
        [ContentHash] nvarchar(128) NULL,
        [CreatedAt] datetime NOT NULL,
        [MimeType] nvarchar(128) NULL,
        [Name] nvarchar(256) NOT NULL,
        [Path] nvarchar(max) NOT NULL,
        [FileOrder] int NOT NULL,
        [VaultID] nvarchar(128) NOT NULL,
        [ProjectID] nvarchar(128) NULL,
        [SessionID] nvarchar(128) NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultFileRefs_VaultHeaders_VaultID] FOREIGN KEY ([VaultID]) REFERENCES [VaultHeaders] ([ID]),
        CONSTRAINT [FK_VaultFileRefs_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_VaultFileRefs_VaultSessions_SessionID] FOREIGN KEY ([SessionID]) REFERENCES [VaultSessions] ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultNotes] (
        [ID] nvarchar(128) NOT NULL,
        [Content] nvarchar(max) NULL,
        [CreatedAt] datetime NOT NULL,
        [EmbeddingJson] nvarchar(max) NULL,
        [IsPinned] bit NOT NULL,
        [IsSynced] bit NOT NULL,
        [IsTemplate] bit NOT NULL,
        [MetadataJson] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [TemplateName] nvarchar(256) NULL,
        [Summary] nvarchar(512) NULL,
        [Title] nvarchar(256) NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [VaultID] nvarchar(128) NOT NULL,
        [ProjectID] nvarchar(128) NULL,
        [SessionID] nvarchar(128) NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultNotes_VaultHeaders_VaultID] FOREIGN KEY ([VaultID]) REFERENCES [VaultHeaders] ([ID]),
        CONSTRAINT [FK_VaultNotes_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_VaultNotes_VaultSessions_SessionID] FOREIGN KEY ([SessionID]) REFERENCES [VaultSessions] ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowNodes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [Instructions] nvarchar(max) NULL,
        [MetadataJson] nvarchar(max) NULL,
        [Name] nvarchar(256) NOT NULL,
        [NodeType] nvarchar(64) NOT NULL,
        [NodeOrder] int NOT NULL,
        [Status] nvarchar(64) NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [WorkflowID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowNodes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowNodes_VaultWorkflows_WorkflowID] FOREIGN KEY ([WorkflowID]) REFERENCES [VaultWorkflows] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultFileRefRelations] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [RelationType] nvarchar(64) NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [Weight] real NOT NULL,
        [SourceFileRefID] nvarchar(128) NOT NULL,
        [TargetFileRefID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultFileRefRelations] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultFileRefRelations_VaultFileRefs_SourceFileRefID] FOREIGN KEY ([SourceFileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultFileRefRelations_VaultFileRefs_TargetFileRefID] FOREIGN KEY ([TargetFileRefID]) REFERENCES [VaultFileRefs] ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultHeaderFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [VaultID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultHeaderFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultHeaderFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultHeaderFileRefs_VaultHeaders_VaultID] FOREIGN KEY ([VaultID]) REFERENCES [VaultHeaders] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultProjectFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [ProjectID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultProjectFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultProjectFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultProjectFileRefs_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultSessionFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [SessionID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultSessionFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultSessionFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultSessionFileRefs_VaultSessions_SessionID] FOREIGN KEY ([SessionID]) REFERENCES [VaultSessions] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [WorkflowID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultWorkflowFileRefs_VaultWorkflows_WorkflowID] FOREIGN KEY ([WorkflowID]) REFERENCES [VaultWorkflows] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultFileRefNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [NoteOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultFileRefNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultFileRefNotes_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_VaultFileRefNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultHeaderNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [VaultID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultHeaderNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultHeaderNotes_VaultHeaders_VaultID] FOREIGN KEY ([VaultID]) REFERENCES [VaultHeaders] ([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_VaultHeaderNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultMetadata] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Key] nvarchar(256) NOT NULL,
        [OwnerID] nvarchar(128) NULL,
        [UpdatedAt] datetime NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultMetadata] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultMetadata_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultNoteFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultNoteFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultNoteFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultNoteFileRefs_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultNoteRelations] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [RelationType] nvarchar(64) NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [Weight] real NOT NULL,
        [SourceNoteID] nvarchar(128) NOT NULL,
        [TargetNoteID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultNoteRelations] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultNoteRelations_VaultNotes_SourceNoteID] FOREIGN KEY ([SourceNoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultNoteRelations_VaultNotes_TargetNoteID] FOREIGN KEY ([TargetNoteID]) REFERENCES [VaultNotes] ([ID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultNoteVaultTag] (
        [NotesID] nvarchar(128) NOT NULL,
        [TagsID] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_VaultNoteVaultTag] PRIMARY KEY ([NotesID], [TagsID]),
        CONSTRAINT [FK_VaultNoteVaultTag_VaultNotes_NotesID] FOREIGN KEY ([NotesID]) REFERENCES [VaultNotes] ([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_VaultNoteVaultTag_VaultTags_TagsID] FOREIGN KEY ([TagsID]) REFERENCES [VaultTags] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultProjectNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [ProjectID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultProjectNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultProjectNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultProjectNotes_VaultProjects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [VaultProjects] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultSessionNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [SessionID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultSessionNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultSessionNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultSessionNotes_VaultSessions_SessionID] FOREIGN KEY ([SessionID]) REFERENCES [VaultSessions] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [WorkflowID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultWorkflowNotes_VaultWorkflows_WorkflowID] FOREIGN KEY ([WorkflowID]) REFERENCES [VaultWorkflows] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowLines] (
        [ID] nvarchar(128) NOT NULL,
        [ConditionJson] nvarchar(max) NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsDefaultLine] bit NOT NULL,
        [LineType] nvarchar(64) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [LineOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [SourceWorkflowNodeID] nvarchar(128) NOT NULL,
        [TargetWorkflowNodeID] nvarchar(128) NOT NULL,
        [WorkflowID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowLines] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowLines_VaultWorkflowNodes_SourceWorkflowNodeID] FOREIGN KEY ([SourceWorkflowNodeID]) REFERENCES [VaultWorkflowNodes] ([ID]),
        CONSTRAINT [FK_VaultWorkflowLines_VaultWorkflowNodes_TargetWorkflowNodeID] FOREIGN KEY ([TargetWorkflowNodeID]) REFERENCES [VaultWorkflowNodes] ([ID]),
        CONSTRAINT [FK_VaultWorkflowLines_VaultWorkflows_WorkflowID] FOREIGN KEY ([WorkflowID]) REFERENCES [VaultWorkflows] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowNodeFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [WorkflowNodeID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowNodeFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowNodeFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultWorkflowNodeFileRefs_VaultWorkflowNodes_WorkflowNodeID] FOREIGN KEY ([WorkflowNodeID]) REFERENCES [VaultWorkflowNodes] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowNodeNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [NoteOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [WorkflowNodeID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowNodeNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowNodeNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultWorkflowNodeNotes_VaultWorkflowNodes_WorkflowNodeID] FOREIGN KEY ([WorkflowNodeID]) REFERENCES [VaultWorkflowNodes] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowLineFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [FileOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [WorkflowLineID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowLineFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineFileRefs_VaultWorkflowLines_WorkflowLineID] FOREIGN KEY ([WorkflowLineID]) REFERENCES [VaultWorkflowLines] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowLineNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [WorkflowLineID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowLineNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineNotes_VaultWorkflowLines_WorkflowLineID] FOREIGN KEY ([WorkflowLineID]) REFERENCES [VaultWorkflowLines] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowLineSteps] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Description] nvarchar(max) NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [StepOrder] int NOT NULL,
        [StepType] nvarchar(64) NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [WorkflowLineID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowLineSteps] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineSteps_VaultWorkflowLines_WorkflowLineID] FOREIGN KEY ([WorkflowLineID]) REFERENCES [VaultWorkflowLines] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowLineStepFileRefs] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [FileRefID] nvarchar(128) NOT NULL,
        [WorkflowLineStepID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowLineStepFileRefs] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineStepFileRefs_VaultFileRefs_FileRefID] FOREIGN KEY ([FileRefID]) REFERENCES [VaultFileRefs] ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineStepFileRefs_VaultWorkflowLineSteps_WorkflowLineStepID] FOREIGN KEY ([WorkflowLineStepID]) REFERENCES [VaultWorkflowLineSteps] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE TABLE [VaultWorkflowLineStepNotes] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [UsageRole] nvarchar(64) NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [WorkflowLineStepID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultWorkflowLineStepNotes] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineStepNotes_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultWorkflowLineStepNotes_VaultWorkflowLineSteps_WorkflowLineStepID] FOREIGN KEY ([WorkflowLineStepID]) REFERENCES [VaultWorkflowLineSteps] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_AvaModelDefinitions_ProviderProfileID] ON [AvaModelDefinitions] ([ProviderProfileID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_AvaModelDefinitions_ProviderProfileId_SortOrder] ON [AvaModelDefinitions] ([ProviderProfileID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AvaModelDefinitions_ProviderProfileId_ModelId] ON [AvaModelDefinitions] ([ProviderProfileID], [ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_AvaProviderProfiles_Name] ON [AvaProviderProfiles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_AvaProviderProfiles_ProviderType] ON [AvaProviderProfiles] ([ProviderType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_AvaProviderProfiles_SortOrder] ON [AvaProviderProfiles] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefNotes_FileRefID] ON [VaultFileRefNotes] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefNotes_FileRefId_SortOrder] ON [VaultFileRefNotes] ([FileRefID], [NoteOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefNotes_NoteID] ON [VaultFileRefNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultFileRefNotes_FileRefId_NoteID] ON [VaultFileRefNotes] ([FileRefID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefRelations_SourceFileRefID] ON [VaultFileRefRelations] ([SourceFileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefRelations_SourceFileRefId_SortOrder] ON [VaultFileRefRelations] ([SourceFileRefID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefRelations_TargetFileRefID] ON [VaultFileRefRelations] ([TargetFileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefs_ProjectID] ON [VaultFileRefs] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefs_SessionID] ON [VaultFileRefs] ([SessionID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultFileRefs_VaultID] ON [VaultFileRefs] ([VaultID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultGraphs_ProjectID] ON [VaultGraphs] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultHeaderFileRefs_FileRefID] ON [VaultHeaderFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultHeaderFileRefs_VaultID] ON [VaultHeaderFileRefs] ([VaultID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultHeaderFileRefs_VaultId_SortOrder] ON [VaultHeaderFileRefs] ([VaultID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultHeaderFileRefs_VaultId_FileRefID] ON [VaultHeaderFileRefs] ([VaultID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultHeaderNotes_NoteID] ON [VaultHeaderNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultHeaderNotes_VaultID] ON [VaultHeaderNotes] ([VaultID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultHeaderNotes_VaultId_SortOrder] ON [VaultHeaderNotes] ([VaultID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultHeaderNotes_VaultId_NoteID] ON [VaultHeaderNotes] ([VaultID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultMetadata_NoteID] ON [VaultMetadata] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultMetadata_NoteId_Key] ON [VaultMetadata] ([NoteID], [Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNoteFileRefs_FileRefID] ON [VaultNoteFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNoteFileRefs_NoteID] ON [VaultNoteFileRefs] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNoteFileRefs_NoteId_SortOrder] ON [VaultNoteFileRefs] ([NoteID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultNoteFileRefs_NoteId_FileRefID] ON [VaultNoteFileRefs] ([NoteID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNoteRelations_SourceNoteID] ON [VaultNoteRelations] ([SourceNoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNoteRelations_SourceNoteId_SortOrder] ON [VaultNoteRelations] ([SourceNoteID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNoteRelations_TargetNoteID] ON [VaultNoteRelations] ([TargetNoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNotes_ProjectID] ON [VaultNotes] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNotes_ProjectId_SortOrder] ON [VaultNotes] ([ProjectID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNotes_SessionID] ON [VaultNotes] ([SessionID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNotes_VaultID] ON [VaultNotes] ([VaultID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultNoteVaultTag_TagsID] ON [VaultNoteVaultTag] ([TagsID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultProjectFileRefs_FileRefID] ON [VaultProjectFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultProjectFileRefs_ProjectID] ON [VaultProjectFileRefs] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultProjectFileRefs_ProjectId_SortOrder] ON [VaultProjectFileRefs] ([ProjectID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultProjectFileRefs_ProjectId_FileRefID] ON [VaultProjectFileRefs] ([ProjectID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultProjectNotes_NoteID] ON [VaultProjectNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultProjectNotes_ProjectID] ON [VaultProjectNotes] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultProjectNotes_ProjectId_SortOrder] ON [VaultProjectNotes] ([ProjectID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultProjectNotes_ProjectId_NoteID] ON [VaultProjectNotes] ([ProjectID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultProjects_VaultID] ON [VaultProjects] ([VaultID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessionFileRefs_FileRefID] ON [VaultSessionFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessionFileRefs_SessionID] ON [VaultSessionFileRefs] ([SessionID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessionFileRefs_SessionId_SortOrder] ON [VaultSessionFileRefs] ([SessionID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultSessionFileRefs_SessionId_FileRefID] ON [VaultSessionFileRefs] ([SessionID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessionNotes_NoteID] ON [VaultSessionNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessionNotes_SessionID] ON [VaultSessionNotes] ([SessionID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessionNotes_SessionId_SortOrder] ON [VaultSessionNotes] ([SessionID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultSessionNotes_SessionId_NoteID] ON [VaultSessionNotes] ([SessionID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessions_ProjectID] ON [VaultSessions] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessions_ProjectId_SortOrder] ON [VaultSessions] ([ProjectID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultSessions_VaultID] ON [VaultSessions] ([VaultID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultTags_ProjectID] ON [VaultTags] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultTags_ProjectId_Name] ON [VaultTags] ([ProjectID], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowFileRefs_FileRefID] ON [VaultWorkflowFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowFileRefs_WorkflowID] ON [VaultWorkflowFileRefs] ([WorkflowID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowFileRefs_WorkflowId_SortOrder] ON [VaultWorkflowFileRefs] ([WorkflowID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowFileRefs_WorkflowId_FileRefID] ON [VaultWorkflowFileRefs] ([WorkflowID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineFileRefs_FileRefID] ON [VaultWorkflowLineFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineFileRefs_WorkflowLineID] ON [VaultWorkflowLineFileRefs] ([WorkflowLineID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineFileRefs_WorkflowLineId_SortOrder] ON [VaultWorkflowLineFileRefs] ([WorkflowLineID], [FileOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowLineFileRefs_WorkflowLineId_FileRefID] ON [VaultWorkflowLineFileRefs] ([WorkflowLineID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineNotes_NoteID] ON [VaultWorkflowLineNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineNotes_WorkflowLineID] ON [VaultWorkflowLineNotes] ([WorkflowLineID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineNotes_WorkflowLineId_SortOrder] ON [VaultWorkflowLineNotes] ([WorkflowLineID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowLineNotes_WorkflowLineId_NoteID] ON [VaultWorkflowLineNotes] ([WorkflowLineID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLines_SourceWorkflowNodeID] ON [VaultWorkflowLines] ([SourceWorkflowNodeID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLines_SourceWorkflowNodeId_SortOrder] ON [VaultWorkflowLines] ([SourceWorkflowNodeID], [LineOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLines_TargetWorkflowNodeID] ON [VaultWorkflowLines] ([TargetWorkflowNodeID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLines_WorkflowID] ON [VaultWorkflowLines] ([WorkflowID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineStepFileRefs_FileRefID] ON [VaultWorkflowLineStepFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineStepFileRefs_WorkflowLineStepID] ON [VaultWorkflowLineStepFileRefs] ([WorkflowLineStepID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineStepFileRefs_WorkflowLineStepId_SortOrder] ON [VaultWorkflowLineStepFileRefs] ([WorkflowLineStepID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowLineStepFileRefs_WorkflowLineStepId_FileRefID] ON [VaultWorkflowLineStepFileRefs] ([WorkflowLineStepID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineStepNotes_NoteID] ON [VaultWorkflowLineStepNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineStepNotes_WorkflowLineStepID] ON [VaultWorkflowLineStepNotes] ([WorkflowLineStepID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineStepNotes_WorkflowLineStepId_SortOrder] ON [VaultWorkflowLineStepNotes] ([WorkflowLineStepID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowLineStepNotes_WorkflowLineStepId_NoteID] ON [VaultWorkflowLineStepNotes] ([WorkflowLineStepID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowLineSteps_WorkflowLineID] ON [VaultWorkflowLineSteps] ([WorkflowLineID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowLineSteps_WorkflowLineId_StepOrder] ON [VaultWorkflowLineSteps] ([WorkflowLineID], [StepOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodeFileRefs_FileRefID] ON [VaultWorkflowNodeFileRefs] ([FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodeFileRefs_WorkflowNodeID] ON [VaultWorkflowNodeFileRefs] ([WorkflowNodeID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodeFileRefs_WorkflowNodeId_SortOrder] ON [VaultWorkflowNodeFileRefs] ([WorkflowNodeID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowNodeFileRefs_WorkflowNodeId_FileRefID] ON [VaultWorkflowNodeFileRefs] ([WorkflowNodeID], [FileRefID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodeNotes_NoteID] ON [VaultWorkflowNodeNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodeNotes_WorkflowNodeID] ON [VaultWorkflowNodeNotes] ([WorkflowNodeID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodeNotes_WorkflowNodeId_SortOrder] ON [VaultWorkflowNodeNotes] ([WorkflowNodeID], [NoteOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowNodeNotes_WorkflowNodeId_NoteID] ON [VaultWorkflowNodeNotes] ([WorkflowNodeID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodes_WorkflowID] ON [VaultWorkflowNodes] ([WorkflowID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNodes_WorkflowId_SortOrder] ON [VaultWorkflowNodes] ([WorkflowID], [NodeOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNotes_NoteID] ON [VaultWorkflowNotes] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNotes_WorkflowID] ON [VaultWorkflowNotes] ([WorkflowID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflowNotes_WorkflowId_SortOrder] ON [VaultWorkflowNotes] ([WorkflowID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultWorkflowNotes_WorkflowId_NoteID] ON [VaultWorkflowNotes] ([WorkflowID], [NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflows_ProjectID] ON [VaultWorkflows] ([ProjectID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    CREATE INDEX [IX_VaultWorkflows_ProjectId_SortOrder] ON [VaultWorkflows] ([ProjectID], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527174306_IntialMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260527174306_IntialMigration', N'9.0.9');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527180033_VaultNoteVaultTagExplicit'
)
BEGIN
    DROP TABLE [VaultNoteVaultTag];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527180033_VaultNoteVaultTagExplicit'
)
BEGIN
    CREATE TABLE [VaultNoteVaultTags] (
        [ID] nvarchar(128) NOT NULL,
        [CreatedAt] datetime NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime NOT NULL,
        [NoteID] nvarchar(128) NOT NULL,
        [TagID] nvarchar(128) NOT NULL,
        [PrimaryIdentityId] nvarchar(128) NOT NULL,
        [PrimaryIdentityHandle] nvarchar(64) NOT NULL,
        [PrimaryIdentityType] nvarchar(32) NOT NULL,
        [IdentityList] VARBINARY(MAX) NULL,
        CONSTRAINT [PK_VaultNoteVaultTags] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_VaultNoteVaultTags_VaultNotes_NoteID] FOREIGN KEY ([NoteID]) REFERENCES [VaultNotes] ([ID]),
        CONSTRAINT [FK_VaultNoteVaultTags_VaultTags_TagID] FOREIGN KEY ([TagID]) REFERENCES [VaultTags] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527180033_VaultNoteVaultTagExplicit'
)
BEGIN
    CREATE INDEX [IX_VaultNoteVaultTags_NoteID] ON [VaultNoteVaultTags] ([NoteID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527180033_VaultNoteVaultTagExplicit'
)
BEGIN
    CREATE INDEX [IX_VaultNoteVaultTags_TagID] ON [VaultNoteVaultTags] ([TagID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527180033_VaultNoteVaultTagExplicit'
)
BEGIN
    CREATE UNIQUE INDEX [UX_VaultNoteVaultTags_NoteID_TagID] ON [VaultNoteVaultTags] ([NoteID], [TagID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527180033_VaultNoteVaultTagExplicit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260527180033_VaultNoteVaultTagExplicit', N'9.0.9');
END;

COMMIT;
GO

