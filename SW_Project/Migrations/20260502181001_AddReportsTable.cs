using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReporterId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "TargetListingId",
                table: "Reports",
                newName: "ReportedListingId");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Reports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "Reports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReportedUserId",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReporterUserId",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Reports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedByAdminId",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedListingId",
                table: "Reports",
                column: "ReportedListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedUserId",
                table: "Reports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUserId",
                table: "Reports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ResolvedByAdminId",
                table: "Reports",
                column: "ResolvedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedUserId",
                table: "Reports",
                column: "ReportedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ReporterUserId",
                table: "Reports",
                column: "ReporterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedByAdminId",
                table: "Reports",
                column: "ResolvedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Listings_ReportedListingId",
                table: "Reports",
                column: "ReportedListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ReporterUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedByAdminId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Listings_ReportedListingId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportedListingId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportedUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ResolvedByAdminId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReportedUserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReporterUserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResolvedByAdminId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "ReportedListingId",
                table: "Reports",
                newName: "TargetListingId");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "ReporterId",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Reports",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetUserId",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
