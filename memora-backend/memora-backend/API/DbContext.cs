using Microsoft.EntityFrameworkCore;
using Memora.Api.Models;

namespace Memora.Api.Data;

public class MemoraDbContext : DbContext
{
    public MemoraDbContext(DbContextOptions<MemoraDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}