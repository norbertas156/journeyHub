using JourneyHub.Common.Models.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JourneyHub.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public DbSet<Trip>? Trips { get; set; }
        public DbSet<TripRating>? TripRatings { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.OwnsOne(t => t.Area);
                entity.OwnsMany(t => t.MapPoints);
                entity.OwnsMany(t => t.MapMarkers);
            });
        }
    }
}