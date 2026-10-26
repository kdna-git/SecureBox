using Demo.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Demo.EntityFramework;

public sealed class DemoDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DemoObjDto> DemoObjects { get; set; }
}
