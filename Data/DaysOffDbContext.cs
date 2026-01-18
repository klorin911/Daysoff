using DaysOff.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DaysOff.Data;

public sealed class DaysOffDbContext : DbContext
{
    public DaysOffDbContext(DbContextOptions<DaysOffDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserScheduleSelection> UserScheduleSelections => Set<UserScheduleSelection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<UserScheduleSelection>(entity =>
        {
            entity.HasKey(x => x.UserId);

            entity.HasOne(x => x.User)
                .WithOne(x => x.ScheduleSelection)
                .HasForeignKey<UserScheduleSelection>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
