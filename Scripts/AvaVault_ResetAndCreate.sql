-- ============================================================
-- AVA Vault: Drop all existing tables, then recreate from scratch
-- Run this in SSMS or sqlcmd with sufficient permissions
-- ============================================================

-- Drop all foreign key constraints first, then tables
DECLARE @sql NVARCHAR(MAX) = N'';

-- Drop all foreign keys
SELECT @sql += N'
ALTER TABLE [' + OBJECT_SCHEMA_NAME(fk.parent_object_id) + '].[' + OBJECT_NAME(fk.parent_object_id) + '] DROP CONSTRAINT [' + fk.name + '];'
FROM sys.foreign_keys AS fk;

-- Drop all user tables (except __EFMigrationsHistory)
SELECT @sql += N'
DROP TABLE [' + OBJECT_SCHEMA_NAME(t.object_id) + '].[' + t.name + '];'
FROM sys.tables AS t
WHERE t.name != '__EFMigrationsHistory';

EXEC sp_executesql @sql;
GO

-- Now apply the full schema
:r .\AvaVault_FullSchema.sql
GO

PRINT 'Database schema created successfully.';
GO
