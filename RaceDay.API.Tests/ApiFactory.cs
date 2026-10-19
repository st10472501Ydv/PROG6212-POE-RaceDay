using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;

namespace RaceDay.API.Tests;

// Boots the real API in memory so the tests run without SQL Server (CI has none).
// Each factory gets its own database name, so test classes cannot see each other's data.
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        string dbName = $"RaceDayTests-{Guid.NewGuid()}";
        builder.ConfigureServices(services =>
        {
            // Remove everything Program.cs registered for SQL Server before adding InMemory,
            // otherwise EF sees two providers and refuses to start.
            services.RemoveAll(typeof(DbContextOptions<RaceDayContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<RaceDayContext>));
            services.AddDbContext<RaceDayContext>(options => options.UseInMemoryDatabase(dbName));
        });
    }
}
