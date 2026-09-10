using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcikIstihbarat.API.Migrations
{
    /// <inheritdoc />
    public partial class ResetAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-off: reset the seeded 'admin' user's password to a new value since there is
            // no admin-panel UI for this yet. Uses ASP.NET Core Identity's own PasswordHasher
            // so the stored hash format matches exactly what UserManager.CheckPasswordAsync
            // expects.
            var hasher = new PasswordHasher<IdentityUser>();
            var hash = hasher.HashPassword(new IdentityUser(), "Acik!2026#Yonetim7Zq");
            migrationBuilder.Sql($"UPDATE AspNetUsers SET PasswordHash = '{hash}' WHERE UserName = 'admin';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: original password hash is not recoverable.
        }
    }
}
