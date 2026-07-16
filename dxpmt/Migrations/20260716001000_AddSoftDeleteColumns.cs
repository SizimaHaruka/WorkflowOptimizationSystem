using dxpmt.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dxpmt.Migrations;

/// <summary>
/// Ensures databases created before EF migrations gained the soft-delete columns.
/// The conditional SQL makes this safe after <see cref="InitialCreate"/> as well.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260716001000_AddSoftDeleteColumns")]
public partial class AddSoftDeleteColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'[dbo].[WorkItems]', N'IsDeleted') IS NULL
                ALTER TABLE [dbo].[WorkItems] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_WorkItems_IsDeleted] DEFAULT 0;

            IF COL_LENGTH(N'[dbo].[WorkItems]', N'DeletedAt') IS NULL
                ALTER TABLE [dbo].[WorkItems] ADD [DeletedAt] datetime2 NULL;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH(N'[dbo].[Problems]', N'IsDeleted') IS NULL
                ALTER TABLE [dbo].[Problems] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_Problems_IsDeleted] DEFAULT 0;

            IF COL_LENGTH(N'[dbo].[Problems]', N'DeletedAt') IS NULL
                ALTER TABLE [dbo].[Problems] ADD [DeletedAt] datetime2 NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[DF_WorkItems_IsDeleted]', N'D') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[WorkItems] DROP CONSTRAINT [DF_WorkItems_IsDeleted];
                ALTER TABLE [dbo].[WorkItems] DROP COLUMN [IsDeleted];
                ALTER TABLE [dbo].[WorkItems] DROP COLUMN [DeletedAt];
            END
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[DF_Problems_IsDeleted]', N'D') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Problems] DROP CONSTRAINT [DF_Problems_IsDeleted];
                ALTER TABLE [dbo].[Problems] DROP COLUMN [IsDeleted];
                ALTER TABLE [dbo].[Problems] DROP COLUMN [DeletedAt];
            END
            """);
    }
}
