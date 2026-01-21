using AuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
<<<<<<< HEAD
        // Users
=======
>>>>>>> origin/main
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });
<<<<<<< HEAD

        // Groups
        modelBuilder.Entity<GroupMember>().HasKey(x => new { x.GroupId, x.UserId });
        modelBuilder.Entity<MemoryTag>().HasKey(x => new { x.MemoryId, x.Value });

        modelBuilder.Entity<Group>()
            .HasMany(x => x.Members)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<Group>()
            .HasMany(x => x.Memories)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId);
=======
>>>>>>> origin/main
    }
}
