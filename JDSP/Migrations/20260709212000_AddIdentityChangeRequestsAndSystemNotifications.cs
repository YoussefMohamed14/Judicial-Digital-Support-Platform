using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDSP.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityChangeRequestsAndSystemNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CurrentFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequestedFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrentPhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RequestedPhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CurrentNationalNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedNationalNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LegalIdFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LegalIdFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityChangeRequests_AspNetUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentityChangeRequests_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemNotifications_AspNetUsers_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityChangeRequests_RequestedById",
                table: "IdentityChangeRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityChangeRequests_ReviewedById",
                table: "IdentityChangeRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityChangeRequests_Status_RequestedAt",
                table: "IdentityChangeRequests",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_RecipientId_IsRead_CreatedAt",
                table: "SystemNotifications",
                columns: new[] { "RecipientId", "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityChangeRequests");

            migrationBuilder.DropTable(
                name: "SystemNotifications");
        }
    }
}
