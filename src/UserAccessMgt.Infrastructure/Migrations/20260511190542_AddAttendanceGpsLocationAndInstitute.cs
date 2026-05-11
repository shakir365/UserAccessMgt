using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserAccessMgt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceGpsLocationAndInstitute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_UserId",
                table: "Attendances");

            migrationBuilder.AddColumn<double>(
                name: "CheckInLatitude",
                table: "Attendances",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckInLongitude",
                table: "Attendances",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckOutLatitude",
                table: "Attendances",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckOutLongitude",
                table: "Attendances",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstituteId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_InstituteId",
                table: "Attendances",
                column: "InstituteId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SubmittedByUserId",
                table: "Attendances",
                column: "SubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Institutes_InstituteId",
                table: "Attendances",
                column: "InstituteId",
                principalTable: "Institutes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_SubmittedByUserId",
                table: "Attendances",
                column: "SubmittedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_UserId",
                table: "Attendances",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Institutes_InstituteId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_SubmittedByUserId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_UserId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_InstituteId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_SubmittedByUserId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInLatitude",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInLongitude",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckOutLatitude",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckOutLongitude",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "InstituteId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Attendances");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_UserId",
                table: "Attendances",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
