using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminToContactMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminId",
                table: "ContactMessages",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_AdminId",
                table: "ContactMessages",
                column: "AdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactMessages_AspNetUsers_AdminId",
                table: "ContactMessages",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactMessages_AspNetUsers_AdminId",
                table: "ContactMessages");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_AdminId",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "ContactMessages");
        }
    }
}
