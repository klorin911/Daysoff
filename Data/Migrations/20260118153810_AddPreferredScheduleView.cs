using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaysOff.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredScheduleView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredScheduleView",
                table: "UserScheduleSelections",
                type: "TEXT",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredScheduleView",
                table: "UserScheduleSelections");
        }
    }
}
