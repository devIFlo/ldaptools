using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LdapTools.Migrations
{
    /// <inheritdoc />
    public partial class AddResetPasswordUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "PasswordResetTokens",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "PasswordResetTokens");
        }
    }
}
