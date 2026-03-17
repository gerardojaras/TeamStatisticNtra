using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Team_Mascot_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TeamRecords_Team_Mascot",
                table: "TeamRecords",
                columns: new[] { "Team", "Mascot" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamRecords_Team_Mascot",
                table: "TeamRecords");
        }
    }
}
