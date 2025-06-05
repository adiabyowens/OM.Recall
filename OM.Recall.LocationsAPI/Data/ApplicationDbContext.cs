using Microsoft.EntityFrameworkCore;
using OM.Recall.LocationsAPI.Models;

namespace OM.Recall.LocationsAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Location> Locations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Create unique index on Identifier
            modelBuilder.Entity<Location>()
                .HasIndex(l => l.Identifier)
                .IsUnique()
                .HasDatabaseName("IX_Locations_Identifier");

            // Configure string lengths and requirements
            modelBuilder.Entity<Location>(entity =>
            {
                entity.Property(e => e.Identifier)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Description)
                    .HasMaxLength(100);

                entity.Property(e => e.SystemTypeName)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasDefaultValue("CSW");

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}