using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcikIstihbarat.API.Migrations
{
    /// <inheritdoc />
    public partial class BackfillConfirmedAtForActiveSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only fix: ConfirmedAt/LastConfirmedAt were added in the previous migration,
            // so pre-existing subscribers who confirmed BEFORE that migration ran have IsActive=1
            // but ConfirmedAt/LastConfirmedAt still NULL, hiding them from the admin subscribers
            // list. Backfill using CreatedAt as the best available approximation of the
            // confirmation date. Rows that were never actually confirmed (IsActive=0 and
            // ConfirmedAt still NULL) are intentionally left untouched.
            migrationBuilder.Sql(@"
                UPDATE MailSubscribers
                SET ConfirmedAt = CreatedAt,
                    LastConfirmedAt = CreatedAt
                WHERE IsActive = 1 AND ConfirmedAt IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: original confirmation dates for backfilled rows are unknown.
        }
    }
}
