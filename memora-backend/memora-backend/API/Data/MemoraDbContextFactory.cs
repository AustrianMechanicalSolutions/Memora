using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Memora.Api.Data;

public class MemoraDbContextFactory : IDesignTimeDbContextFactory<MemoraDbContext>
{
    public MemoraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MemoraDbContext>();

        optionsBuilder.UseSqlite("Data Source=memora.db");

        return new MemoraDbContext(optionsBuilder.Options);
    }
}
