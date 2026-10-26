using Microsoft.EntityFrameworkCore;

namespace Demo.EntityFramework;

public sealed class DemoDbContextFactory(DbContextOptions options)
{
    public DemoDbContext Create()
    {
        return new DemoDbContext(options);
    }
}
