using dxpmt.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dxpmt.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260720040000_NormalizeConfirmedCaseFormStatus")]
public partial class NormalizeConfirmedCaseFormStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE formRecord
            SET Status = N'確認済み',
                ConfirmedBy = baseline.ConfirmedBy,
                ConfirmedAt = baseline.ConfirmedAt
            FROM CaseForms AS formRecord
            CROSS APPLY (
                SELECT TOP (1) ConfirmedBy, ConfirmedAt
                FROM GateBaselines
                WHERE CaseId = formRecord.CaseId
                  AND FormType = formRecord.FormType
                ORDER BY ConfirmedAt DESC, Id DESC
            ) AS baseline
            WHERE formRecord.FormType IN (N'F01', N'F02', N'F04', N'F09')
              AND formRecord.UpdatedAt <= baseline.ConfirmedAt;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
