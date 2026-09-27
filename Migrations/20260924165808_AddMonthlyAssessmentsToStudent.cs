using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onudhabon_ISD.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyAssessmentsToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MonthlyAssessmentsJson",
                table: "Students",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyAssessmentsJson",
                table: "Students");
        }
    }
}
