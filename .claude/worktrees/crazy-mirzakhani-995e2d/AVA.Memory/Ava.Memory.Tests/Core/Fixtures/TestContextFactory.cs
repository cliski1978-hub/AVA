using AVA.Memory.Sql.Context;
using Microsoft.EntityFrameworkCore;

public class TestContextFactory : IDbContextFactory<MemoryDbContext>
{
    private readonly DbContextOptions<MemoryDbContext> _options;
    public TestContextFactory(DbContextOptions<MemoryDbContext> options)
        => _options = options;

    public MemoryDbContext CreateDbContext() => new MemoryDbContext(_options);
}
