using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProjectQuang.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessageDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Properties_PropertyId",
                table: "Messages");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Properties_PropertyId",
                table: "Messages",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "PropertyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Properties_PropertyId",
                table: "Messages");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Properties_PropertyId",
                table: "Messages",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "PropertyId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
