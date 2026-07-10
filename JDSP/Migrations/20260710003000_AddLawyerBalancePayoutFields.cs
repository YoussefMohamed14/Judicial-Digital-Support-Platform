using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDSP.Migrations
{
    /// <inheritdoc />
    public partial class AddLawyerBalancePayoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LawyerPayoutStatus",
                table: "Payments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.AddColumn<DateTime>(
                name: "LawyerPayoutRequestedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LawyerPayoutCompletedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LawyerPayoutCardLast4",
                table: "Payments",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LawyerPayoutReference",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequestedByLawyerId_Status_LawyerPayoutStatus",
                table: "Payments",
                columns: new[] { "RequestedByLawyerId", "Status", "LawyerPayoutStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_RequestedByLawyerId_Status_LawyerPayoutStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LawyerPayoutStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LawyerPayoutRequestedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LawyerPayoutCompletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LawyerPayoutCardLast4",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LawyerPayoutReference",
                table: "Payments");
        }
    }
}
