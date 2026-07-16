using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dxpmt.Migrations
{
    /// <inheritdoc />
    public partial class SetExistingWorkItemsToAsIs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkType",
                table: "WorkItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "AsIs",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.Sql("UPDATE [WorkItems] SET [WorkType] = N'AsIs' WHERE [WorkType] IS NULL OR [WorkType] = N'';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkType",
                table: "WorkItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValue: "AsIs");
        }
    }
}
