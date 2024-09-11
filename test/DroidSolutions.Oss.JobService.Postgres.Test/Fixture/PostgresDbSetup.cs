using System;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace DroidSolutions.Oss.JobService.Postgres.Test.Fixture;

public class PostgresDbSetup : IAsyncLifetime
{
  public PostgresDbSetup()
  {
    IConfigurationRoot config = new ConfigurationBuilder()
      .AddJsonFile("appsettings.Test.json", false)
      .AddJsonFile("appsettings.LocalTest.json", true)
      .AddEnvironmentVariables()
      .Build();

    var factory = new LoggerFactory();
    factory.AddProvider(new ConsoleLoggerProvider());

    DbContextOptionsBuilder<TestContext>? dbContextBuilder = new DbContextOptionsBuilder<TestContext>()
      .UseNpgsql(config.GetConnectionString("DataContext"))
      // .UseLoggerFactory(factory)
      .EnableSensitiveDataLogging(true);

    Context = new TestContext(dbContextBuilder.Options);
  }

  public TestContext Context { get; }

  public void Dispose()
  {
    Context?.Dispose();
  }

  public Task DisposeAsync()
  {
    Context?.Dispose();

    return Task.CompletedTask;
  }

  public async Task InitializeAsync()
  {
    // clear test db by removing it and re-creating it
    if (Environment.GetEnvironmentVariable("CI") == "true")
    {
      // skip delete in CI and set up the db without migration
      var sql = Context.Database.GenerateCreateScript();
      await Context.Database.ExecuteSqlRawAsync(sql);
    }
    else
    {
      await Context.Database.EnsureDeletedAsync();
      await Context.Database.EnsureCreatedAsync();
    }
  }
}
