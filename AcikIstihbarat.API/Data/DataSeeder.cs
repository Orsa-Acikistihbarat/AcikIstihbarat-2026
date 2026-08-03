using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                var user = new IdentityUser
                {
                    UserName = "admin",
                    Email = "admin@acikistihbarat.local",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Admin123!");
                if (result.Succeeded)
                {
                    // For a real app, you would create and assign an "Admin" role here
                }
            }
        }

        public static async Task SeedKategorilerAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            var kategoriler = new Dictionary<int, string>
            {
                { 1, "Siyaset" },
                { 2, "İstihbarat" },
                { 3, "AB" },
                { 4, "ABD" },
                { 5, "Ortadoğu" },
                { 6, "Dünya" },
                { 7, "Ekonomi" },
                { 8, "Dış Politika" },
                { 9, "Asya" },
                { 10, "Jeo-Kritik" },
                { 11, "Küresel Şebekeler" },
                { 12, "Duyurular" },
                { 13, "Tarih/Kültür" },
                { 14, "Teknoloji" },
                { 15, "Medya" },
                { 16, "Yurdum İnsanı" },
                { 17, "İşaret Fişekleri" }
            };

            // Using raw SQL to handle identity insert safely and efficiently, ignoring FKs if we are just replacing text
            // Or better yet, just upsert using EF Core
            
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Temporarily turn off constraints might not work easily in EF, let's just Upsert
                foreach (var kvp in kategoriler)
                {
                    var existing = await context.HaberKategorileri.FindAsync(kvp.Key);
                    var slug = kvp.Value.ToLower().Replace(" ", "-").Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u").Replace("ş", "s").Replace("ö", "o").Replace("ç", "c").Replace("/", "-");
                    
                    if (existing != null)
                    {
                        existing.Ad = kvp.Value;
                        existing.Slug = slug;
                    }
                    else
                    {
                        // EF might throw on identity insert if we just add it, so let's use raw SQL for insertion
                        await context.Database.ExecuteSqlInterpolatedAsync($"SET IDENTITY_INSERT HaberKategorileri ON; INSERT INTO HaberKategorileri (Id, Ad, Slug, Sira) VALUES ({kvp.Key}, {kvp.Value}, {slug}, {kvp.Key}); SET IDENTITY_INSERT HaberKategorileri OFF;");
                    }
                }
                
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
