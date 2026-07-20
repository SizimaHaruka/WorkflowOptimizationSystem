using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dxpmt.Migrations
{
    /// <inheritdoc />
    public partial class AddGateBaselines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GateBaselines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    GateReviewId = table.Column<int>(type: "int", nullable: false),
                    Gate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FormType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateBaselines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GateBaselines_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GateBaselines_GateReviews_GateReviewId",
                        column: x => x.GateReviewId,
                        principalTable: "GateReviews",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GateBaselines_CaseId_FormType_Version",
                table: "GateBaselines",
                columns: new[] { "CaseId", "FormType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GateBaselines_GateReviewId",
                table: "GateBaselines",
                column: "GateReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GateBaselines");
        }
    }
}
