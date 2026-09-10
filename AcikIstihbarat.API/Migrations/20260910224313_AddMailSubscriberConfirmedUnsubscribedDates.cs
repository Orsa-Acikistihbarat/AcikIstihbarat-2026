using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcikIstihbarat.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMailSubscriberConfirmedUnsubscribedDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "MailSubscribers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConfirmedAt",
                table: "MailSubscribers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnsubscribedAt",
                table: "MailSubscribers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "MailSubscribers");

            migrationBuilder.DropColumn(
                name: "LastConfirmedAt",
                table: "MailSubscribers");

            migrationBuilder.DropColumn(
                name: "UnsubscribedAt",
                table: "MailSubscribers");
        }
    }
}
