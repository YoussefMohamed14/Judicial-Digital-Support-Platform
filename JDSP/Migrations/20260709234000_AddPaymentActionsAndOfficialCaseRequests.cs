using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDSP.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentActionsAndOfficialCaseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingType",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "OneTime");

            migrationBuilder.AddColumn<string>(
                name: "DeclineReason",
                table: "Payments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Payments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "Payments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByLawyerId",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OfficialCaseRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    LawyerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HearingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HearingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CourtNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialCaseRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficialCaseRequests_AspNetUsers_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialCaseRequests_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialCaseRequests_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CaseId_Status_RequestedAt",
                table: "Payments",
                columns: new[] { "CaseId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequestedByLawyerId",
                table: "Payments",
                column: "RequestedByLawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialCaseRequests_CaseId_LawyerId_Status",
                table: "OfficialCaseRequests",
                columns: new[] { "CaseId", "LawyerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialCaseRequests_LawyerId",
                table: "OfficialCaseRequests",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialCaseRequests_ReviewedById",
                table: "OfficialCaseRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialCaseRequests_Status_RequestedAt",
                table: "OfficialCaseRequests",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_RequestedByLawyerId",
                table: "Payments",
                column: "RequestedByLawyerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_RequestedByLawyerId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "OfficialCaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CaseId_Status_RequestedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RequestedByLawyerId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BillingType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeclineReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RequestedByLawyerId",
                table: "Payments");
        }
    }
}
