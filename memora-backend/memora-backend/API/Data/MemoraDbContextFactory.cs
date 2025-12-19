using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Memora.Api.Data;

public class MemoraDbContextFactory : IDesignTimeDbContextFactory<MemoraDbContext>
{
    public MemoraDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MemoraDbContext>()
            .UseSqlite("Data Source=memora.db")
            .Options;

        return new MemoraDbContext(options);
    }
}
