using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JDSP.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessagesAndPaymentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReceiverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RelatedCaseId = table.Column<int>(type: "int", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Cases_RelatedCaseId",
                        column: x => x.RelatedCaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_PaymentId",
                table: "ChatMessages",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_RelatedCaseId",
                table: "ChatMessages",
                column: "RelatedCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReceiverId_IsRead_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "ReceiverId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId_ReceiverId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "SenderId", "ReceiverId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");
        }
    }
}
