using Memora.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Memora.Api.Data;

public class MemoraDbContext : DbContext
{
    public MemoraDbContext(DbContextOptions<MemoraDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Memory> Memories => Set<Memory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<MemoryTag> MemoryTags => Set<MemoryTag>();
    public DbSet<MemoryLike> MemoryLikes => Set<MemoryLike>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Many-to-Many: Memory <-> Tag
        modelBuilder.Entity<MemoryTag>()
            .HasKey(mt => new { mt.MemoryId, mt.TagId });

        modelBuilder.Entity<MemoryTag>()
            .HasOne(mt => mt.Memory)
            .WithMany(m => m.MemoryTags)
            .HasForeignKey(mt => mt.MemoryId);

        modelBuilder.Entity<MemoryTag>()
            .HasOne(mt => mt.Tag)
            .WithMany(t => t.MemoryTags)
            .HasForeignKey(mt => mt.TagId);

        // Many-to-Many: Memory <-> User (Likes)
        modelBuilder.Entity<MemoryLike>()
            .HasKey(ml => new { ml.MemoryId, ml.UserId });

        modelBuilder.Entity<MemoryLike>()
            .HasOne(ml => ml.Memory)
            .WithMany(m => m.Likes)
            .HasForeignKey(ml => ml.MemoryId);

        modelBuilder.Entity<MemoryLike>()
            .HasOne(ml => ml.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(ml => ml.UserId);

        // Defaults
        modelBuilder.Entity<Memory>()
            .Property(m => m.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}