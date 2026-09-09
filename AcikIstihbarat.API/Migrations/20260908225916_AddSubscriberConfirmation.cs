using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcikIstihbarat.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriberConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmToken",
                table: "MailSubscribers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmTokenExpiresAt",
                table: "MailSubscribers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "PendingConfirmationEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    ConfirmToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateDisplayNamesCsv = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingConfirmationEmails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailSubscribers_ConfirmToken",
                table: "MailSubscribers",
                column: "ConfirmToken");

            migrationBuilder.CreateIndex(
                name: "IX_PendingConfirmationEmails_SentAt",
                table: "PendingConfirmationEmails",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingConfirmationEmails");

            migrationBuilder.DropIndex(
                name: "IX_MailSubscribers_ConfirmToken",
                table: "MailSubscribers");

            migrationBuilder.DropColumn(
                name: "ConfirmToken",
                table: "MailSubscribers");

            migrationBuilder.DropColumn(
                name: "ConfirmTokenExpiresAt",
                table: "MailSubscribers");
        }
    }
}
