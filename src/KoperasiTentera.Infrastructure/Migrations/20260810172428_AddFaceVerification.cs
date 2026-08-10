using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoperasiTentera.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaceImagePath",
                table: "Registrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFaceVerified",
                table: "Registrations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceImagePath",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "IsFaceVerified",
                table: "Registrations");
        }
    }
}
