using Microsoft.EntityFrameworkCore;

namespace dxpmt.Data;

/// <summary>開発用の既存LocalDBへ、後方互換のある追加テーブルだけを適用する。</summary>
public static class DevelopmentSchemaUpdater
{
    public static void Apply(ApplicationDbContext database)
    {
        database.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[dbo].[WorkItems]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[WorkItems] (
                    [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CaseId] int NOT NULL,
                    [Sequence] int NOT NULL,
                    [BusinessProcessName] nvarchar(200) NOT NULL DEFAULT N'',
                    [Name] nvarchar(200) NOT NULL,
                    [DepartmentAndRole] nvarchar(200) NOT NULL DEFAULT N'',
                    [Performer] nvarchar(100) NOT NULL DEFAULT N'',
                    [Location] nvarchar(200) NOT NULL DEFAULT N'',
                    [IsConfirmed] bit NOT NULL,
                    [IsDeleted] bit NOT NULL DEFAULT 0,
                    [DeletedAt] datetime2 NULL,
                    [ContentJson] nvarchar(max) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [FK_WorkItems_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [IX_WorkItems_CaseId_Sequence] UNIQUE ([CaseId], [Sequence])
                );
            END
            """);

        database.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'[dbo].[WorkItems]', N'IsDeleted') IS NULL
                ALTER TABLE [dbo].[WorkItems] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_WorkItems_IsDeleted] DEFAULT 0;
            IF COL_LENGTH(N'[dbo].[WorkItems]', N'DeletedAt') IS NULL
                ALTER TABLE [dbo].[WorkItems] ADD [DeletedAt] datetime2 NULL;
            """);

        database.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[dbo].[Problems]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Problems] (
                    [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CaseId] int NOT NULL,
                    [WorkItemId] int NULL,
                    [Sequence] int NOT NULL,
                    [Category] nvarchar(30) NOT NULL,
                    [Phenomenon] nvarchar(2000) NOT NULL,
                    [OccurrenceCondition] nvarchar(max) NOT NULL DEFAULT N'',
                    [Impact] nvarchar(max) NOT NULL DEFAULT N'',
                    [CurrentWorkaround] nvarchar(max) NOT NULL DEFAULT N'',
                    [CauseHypothesis] nvarchar(max) NOT NULL DEFAULT N'',
                    [Evidence] nvarchar(max) NOT NULL DEFAULT N'',
                    [Severity] nvarchar(10) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [IsDeleted] bit NOT NULL DEFAULT 0,
                    [DeletedAt] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [FK_Problems_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_Problems_WorkItems_WorkItemId] FOREIGN KEY ([WorkItemId]) REFERENCES [dbo].[WorkItems]([Id]),
                    CONSTRAINT [IX_Problems_CaseId_Sequence] UNIQUE ([CaseId], [Sequence])
                );
                CREATE INDEX [IX_Problems_WorkItemId] ON [dbo].[Problems]([WorkItemId]);
            END
            """);

        database.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'[dbo].[Problems]', N'IsDeleted') IS NULL
                ALTER TABLE [dbo].[Problems] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_Problems_IsDeleted] DEFAULT 0;
            IF COL_LENGTH(N'[dbo].[Problems]', N'DeletedAt') IS NULL
                ALTER TABLE [dbo].[Problems] ADD [DeletedAt] datetime2 NULL;
            """);

        database.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[dbo].[TraceLinks]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[TraceLinks] (
                    [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CaseId] int NOT NULL,
                    [SourceType] nvarchar(30) NOT NULL,
                    [SourceId] int NOT NULL,
                    [TargetType] nvarchar(30) NOT NULL,
                    [TargetId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [FK_TraceLinks_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [dbo].[Cases]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [IX_TraceLinks_Trace] UNIQUE ([CaseId], [SourceType], [SourceId], [TargetType], [TargetId])
                );
            END
            """);
    }
}
