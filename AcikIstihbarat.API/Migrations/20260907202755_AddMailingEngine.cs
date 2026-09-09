using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcikIstihbarat.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMailingEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateBaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FrequencyType = table.Column<int>(type: "int", nullable: false),
                    IntervalValue = table.Column<int>(type: "int", nullable: true),
                    DaysOfWeekMask = table.Column<int>(type: "int", nullable: true),
                    TimeOfDayLocal = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailSubscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TemplateBaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSendStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ConsecutiveFailureCount = table.Column<int>(type: "int", nullable: false),
                    UnsubscribeToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailSubscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailSendLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriberId = table.Column<int>(type: "int", nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: true),
                    TemplateFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSendLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSendLogs_MailSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "MailSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailSendLogs_MailSubscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "MailSubscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_ScheduleId",
                table: "EmailSendLogs",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_SubscriberId",
                table: "EmailSendLogs",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_MailSubscribers_Email_TemplateBaseName",
                table: "MailSubscribers",
                columns: new[] { "Email", "TemplateBaseName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailSubscribers_TemplateBaseName_IsActive",
                table: "MailSubscribers",
                columns: new[] { "TemplateBaseName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MailSubscribers_UnsubscribeToken",
                table: "MailSubscribers",
                column: "UnsubscribeToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSendLogs");

            migrationBuilder.DropTable(
                name: "MailSchedules");

            migrationBuilder.DropTable(
                name: "MailSubscribers");
        }
    }
}
