using Microsoft.EntityFrameworkCore;

namespace SecureBox.EntityFramework;

/// <summary>
/// Creates instances of the application's Entity Framework database context.
/// </summary>
public sealed class SecureBoxDbContextFactory(DbContextOptions options)
{
    /// <summary>
    /// Creates a new database context using the configured options.
    /// </summary>
    /// <returns>A new <see cref="SecureBoxDbContext"/> instance.</returns>
    public SecureBoxDbContext Create()
    {
        return new SecureBoxDbContext(options);
    }
}
