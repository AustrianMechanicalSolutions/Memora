using AuthApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AuthApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Album> Albums => Set<Album>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        // Groups
        modelBuilder.Entity<GroupMember>().HasKey(x => new { x.GroupId, x.UserId });
        modelBuilder.Entity<MemoryTag>().HasKey(x => new { x.MemoryId, x.Value });
        modelBuilder.Entity<AlbumPerson>().HasKey(x => new { x.AlbumId, x.UserId });

        modelBuilder.Entity<Group>()
            .HasMany(x => x.Members)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<Group>()
            .HasMany(x => x.Memories)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<AlbumPerson>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        );

        foreach(var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
            }
        }
    }
}