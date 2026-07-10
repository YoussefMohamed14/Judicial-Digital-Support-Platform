using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDSP.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtEmployeeLawyerVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LawyerApprovalRejectionReason",
                table: "AspNetUsers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LawyerApprovalReviewedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LawyerApprovalReviewedById",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LawyerApprovalStatus",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LawyerVerificationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LawyerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NationalIdFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NationalIdFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LawyerIdFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LawyerIdFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LawyerVerificationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LawyerVerificationRequests_AspNetUsers_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LawyerVerificationRequests_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LawyerVerificationRequests_LawyerId",
                table: "LawyerVerificationRequests",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerVerificationRequests_ReviewedById",
                table: "LawyerVerificationRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerVerificationRequests_Status_RequestedAt",
                table: "LawyerVerificationRequests",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.Sql(@"
                UPDATE u
                SET LawyerApprovalStatus = 'Approved'
                FROM AspNetUsers u
                INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
                INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                WHERE r.Name = 'Lawyer';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LawyerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "LawyerApprovalRejectionReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LawyerApprovalReviewedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LawyerApprovalReviewedById",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LawyerApprovalStatus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");
        }
    }
}
