using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Rank = table.Column<int>(type: "INTEGER", nullable: true),
                    Team = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Mascot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DateOfLastWin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WinningPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    Wins = table.Column<int>(type: "INTEGER", nullable: true),
                    Losses = table.Column<int>(type: "INTEGER", nullable: true),
                    Ties = table.Column<int>(type: "INTEGER", nullable: true),
                    Games = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamRecords");
        }
    }
}
