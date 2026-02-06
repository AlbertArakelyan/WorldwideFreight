using Microsoft.EntityFrameworkCore;
using WorldwideFreight.Models;

namespace WorldwideFreight.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Commodity> Commodities { get; set; }

        public override int SaveChanges()
        {
            ApplyTimestapms();

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTimestapms();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTimestapms()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Prevent CreatedAt from being modified
                    entry.Property(x => x.CreatedAt).IsModified = false;

                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}
