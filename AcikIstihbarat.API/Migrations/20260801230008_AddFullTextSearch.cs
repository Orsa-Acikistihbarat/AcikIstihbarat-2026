using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcikIstihbarat.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'AramaCatalog')
                BEGIN
                    EXEC('CREATE FULLTEXT CATALOG AramaCatalog AS DEFAULT;');
                END", suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE FULLTEXT INDEX ON [Haber](
                    HaberBaslik Language 1055, 
                    HaberOnIzlemeMetni Language 1055, 
                    HaberTamMetin Language 1055
                ) 
                KEY INDEX PK_Haber;", suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE FULLTEXT INDEX ON [Yazi](
                    YaziBaslik Language 1055, 
                    YaziOnIzlemeMetni Language 1055, 
                    YaziTamMetin Language 1055
                ) 
                KEY INDEX PK_Yazi;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FULLTEXT INDEX ON [Haber];", suppressTransaction: true);
            migrationBuilder.Sql("DROP FULLTEXT INDEX ON [Yazi];", suppressTransaction: true);
            migrationBuilder.Sql("DROP FULLTEXT CATALOG AramaCatalog;", suppressTransaction: true);
        }
    }
}
