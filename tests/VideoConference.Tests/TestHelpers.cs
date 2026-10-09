using Microsoft.EntityFrameworkCore;
using VideoConference.Infrastructure.Persistence;

namespace VideoConference.Tests;

public static class TestHelpers
{
    public static AppDbContext CreateInMemoryDbContext(string dbName = "")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: string.IsNullOrEmpty(dbName) ? Guid.NewGuid().ToString() : dbName)
            .Options;

        return new AppDbContext(options);
    }
}
