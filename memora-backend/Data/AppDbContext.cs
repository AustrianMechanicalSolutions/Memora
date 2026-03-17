using AuthApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AuthApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<MemoryLike> MemoryLikes => Set<MemoryLike>();
    public DbSet<MemoryComment> MemoryComments => Set<MemoryComment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();

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
        modelBuilder.Entity<MemoryLike>().HasKey(x => new { x.MemoryId, x.UserId });
        modelBuilder.Entity<CommentLike>().HasKey(x => new { x.CommentId, x.UserId });

        modelBuilder.Entity<Group>()
            .HasMany(x => x.Members)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<Group>()
            .HasMany(x => x.Memories)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId);

        modelBuilder.Entity<MemoryComment>()
            .HasOne(x => x.Memory)
            .WithMany()
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemoryComment>()
            .HasOne(x => x.ParentComment)
            .WithMany()
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MemoryLike>()
            .HasOne(x => x.Memory)
            .WithMany()
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommentLike>()
            .HasOne(x => x.Comment)
            .WithMany()
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

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
