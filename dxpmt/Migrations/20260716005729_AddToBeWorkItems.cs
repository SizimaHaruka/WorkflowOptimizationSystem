using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dxpmt.Migrations
{
    /// <inheritdoc />
    public partial class AddToBeWorkItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkType",
                table: "WorkItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkType",
                table: "WorkItems");
        }
    }
}
