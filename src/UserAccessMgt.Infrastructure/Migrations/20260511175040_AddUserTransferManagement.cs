using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserAccessMgt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTransferManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FromInstituteId = table.Column<int>(type: "int", nullable: false),
                    ToInstituteId = table.Column<int>(type: "int", nullable: false),
                    TransferredById = table.Column<int>(type: "int", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTransfers_Institutes_FromInstituteId",
                        column: x => x.FromInstituteId,
                        principalTable: "Institutes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserTransfers_Institutes_ToInstituteId",
                        column: x => x.ToInstituteId,
                        principalTable: "Institutes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserTransfers_Users_TransferredById",
                        column: x => x.TransferredById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserTransfers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTransfers_FromInstituteId",
                table: "UserTransfers",
                column: "FromInstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransfers_ToInstituteId",
                table: "UserTransfers",
                column: "ToInstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransfers_TransferredById",
                table: "UserTransfers",
                column: "TransferredById");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransfers_UserId",
                table: "UserTransfers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTransfers");
        }
    }
}
