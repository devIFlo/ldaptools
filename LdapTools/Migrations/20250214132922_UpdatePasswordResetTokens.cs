using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LdapTools.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePasswordResetTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FortigateToken",
                table: "PasswordResetTokens",
                newName: "FortigateLogin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FortigateLogin",
                table: "PasswordResetTokens",
                newName: "FortigateToken");
        }
    }
}
