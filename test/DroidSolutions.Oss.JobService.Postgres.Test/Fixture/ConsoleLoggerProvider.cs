using Microsoft.Extensions.Logging;

namespace DroidSolutions.Oss.JobService.Postgres.Test.Fixture;

public class ConsoleLoggerProvider : ILoggerProvider
{
  public ILogger CreateLogger(string categoryName) => new ConsoleLogger(categoryName);

  public void Dispose()
  {
  }
}
