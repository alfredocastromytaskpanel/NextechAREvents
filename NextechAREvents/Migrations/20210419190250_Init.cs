using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NextechAREvents.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InfernoEventId = table.Column<string>(nullable: false),
                    MSGraphEventId = table.Column<string>(nullable: false),
                    OrganizerEmail = table.Column<string>(nullable: false),
                    OrganizerName = table.Column<string>(nullable: false),
                    Subject = table.Column<string>(nullable: false),
                    Body = table.Column<string>(nullable: false),
                    StartTZ = table.Column<string>(nullable: false),
                    EndTZ = table.Column<string>(nullable: false),
                    TimeZone = table.Column<string>(nullable: false),
                    Start = table.Column<DateTime>(nullable: false),
                    End = table.Column<DateTime>(nullable: false),
                    OriginalStart = table.Column<DateTimeOffset>(nullable: false),
                    OriginalStartTimeZone = table.Column<string>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attendees",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(nullable: true),
                    Mail = table.Column<string>(nullable: false),
                    InfernoUserId = table.Column<int>(nullable: true),
                    EventId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendees_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_EventId",
                table: "Attendees",
                column: "EventId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendees");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
