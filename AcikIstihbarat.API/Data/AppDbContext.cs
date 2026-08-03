using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcikIstihbarat.API.Models.Entities;

namespace AcikIstihbarat.API.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Haber> Haberler { get; set; }
        public DbSet<HaberKategori> HaberKategorileri { get; set; }
        public DbSet<Medya> MedyaKutuphanesi { get; set; }
        public DbSet<HaberMedya> HaberMedyalar { get; set; }
        public DbSet<Yazar> Yazarlar { get; set; }
        public DbSet<Yazi> Yazilar { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<HaberMedya>()
                .HasKey(hm => new { hm.HaberId, hm.MedyaId });

            builder.Entity<HaberMedya>()
                .HasOne(hm => hm.Haber)
                .WithMany(h => h.Medyalar)
                .HasForeignKey(hm => hm.HaberId);

            builder.Entity<HaberMedya>()
                .HasOne(hm => hm.Medya)
                .WithMany(m => m.Haberler)
                .HasForeignKey(hm => hm.MedyaId);

            builder.Entity<Haber>().HasQueryFilter(h => !h.SilindiMi);

            builder.Entity<HaberKategori>()
                .HasOne(hk => hk.ParentKategori)
                .WithMany(hk => hk.AltKategoriler)
                .HasForeignKey(hk => hk.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Yazarlar
            builder.Entity<Yazar>().HasData(
                new Yazar { YazarID = 1, YazarAdi = "Behiç Gürcihan" },
                new Yazar { YazarID = 13, YazarAdi = "Fatma Sibel Yüksek" }
            );

            builder.Entity<Yazi>()
                .HasOne(y => y.Yazar)
                .WithMany(y => y.Yazilar)
                .HasForeignKey(y => y.YazarId);
        }
    }
}
