using Microsoft.EntityFrameworkCore;
//using SecureBox.Domain.DTOs;

namespace SecureBox.EntityFramework;

/// <summary>
/// Entity Framework Core database context for the application.
/// Defines the database model and provides access to its entities.
/// </summary>
public sealed class SecureBoxDbContext(DbContextOptions options) : DbContext(options)
{
    // Add a DbSet for each entity that should be managed by the database.
    //public DbSet<ObjectDto> Objects { get; set; }
}
