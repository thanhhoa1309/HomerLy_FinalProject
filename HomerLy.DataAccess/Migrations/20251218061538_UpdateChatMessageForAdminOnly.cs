using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomerLy.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChatMessageForAdminOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Accounts_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Tenancies_TenancyId",
                table: "ChatMessages");

            migrationBuilder.RenameColumn(
                name: "TenancyId",
                table: "ChatMessages",
                newName: "ReceiverId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessages_TenancyId",
                table: "ChatMessages",
                newName: "IX_ChatMessages_ReceiverId");

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "ChatMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Accounts_ReceiverId",
                table: "ChatMessages",
                column: "ReceiverId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Accounts_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Accounts_ReceiverId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Accounts_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "ChatMessages");

            migrationBuilder.RenameColumn(
                name: "ReceiverId",
                table: "ChatMessages",
                newName: "TenancyId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessages_ReceiverId",
                table: "ChatMessages",
                newName: "IX_ChatMessages_TenancyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Accounts_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Tenancies_TenancyId",
                table: "ChatMessages",
                column: "TenancyId",
                principalTable: "Tenancies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
