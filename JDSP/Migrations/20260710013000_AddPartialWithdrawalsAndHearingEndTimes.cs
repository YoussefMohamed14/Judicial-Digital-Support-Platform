using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDSP.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialWithdrawalsAndHearingEndTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LawyerWithdrawnAmount",
                table: "Payments",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE Payments SET LawyerWithdrawnAmount = Amount WHERE LawyerPayoutStatus = 'Withdrawn'");

            migrationBuilder.AddColumn<DateTime>(
                name: "HearingEndDate",
                table: "OfficialCaseRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CourtFollowUpNotifiedAt",
                table: "Hearings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Hearings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE Hearings SET EndDate = DATEADD(hour, 2, HearingDate) WHERE EndDate IS NULL");
            migrationBuilder.Sql("UPDATE OfficialCaseRequests SET HearingEndDate = DATEADD(hour, 2, HearingDate) WHERE HearingDate IS NOT NULL AND HearingEndDate IS NULL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Hearings",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(hour, 2, GETDATE())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LawyerWithdrawnAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "HearingEndDate",
                table: "OfficialCaseRequests");

            migrationBuilder.DropColumn(
                name: "CourtFollowUpNotifiedAt",
                table: "Hearings");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Hearings");
        }
    }
}
