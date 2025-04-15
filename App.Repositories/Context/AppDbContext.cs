using App.Repositories.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Blog> Blogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.UseSnakeCaseNames();

            // foreach (var entityType in builder.Model.GetEntityTypes())
            // {
            //     var tableName = entityType.GetTableName()!;
            //     if (tableName.StartsWith("aspnet"))
            //         entityType.SetTableName(tableName.Substring(6).ToLower());
            // }

            builder.Entity<Blog>(entity =>
            {
                entity
                    .HasOne(b => b.AppUser)
                    .WithMany()
                    .HasForeignKey(b => b.UserId);
            });
        }
    }
}
