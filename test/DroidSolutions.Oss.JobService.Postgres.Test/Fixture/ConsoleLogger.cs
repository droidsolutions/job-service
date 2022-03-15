using System;

using Microsoft.Extensions.Logging;

namespace DroidSolutions.Oss.JobService.Postgres.Test.Fixture;

public class ConsoleLogger : ILogger
{
  private readonly string _categoryName;

  public ConsoleLogger(string categoryName)
  {
    _categoryName = categoryName;
  }

  public IDisposable BeginScope<TState>(TState state) => NoopDisposable.Instance;

  public bool IsEnabled(LogLevel logLevel) => (int)logLevel >= 2;

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
  {
    Console.WriteLine("{0} [{1}] {2}", _categoryName, eventId, formatter(state, exception));

    if (exception != null)
    {
      Console.WriteLine(exception.ToString());
    }
  }

  private class NoopDisposable : IDisposable
  {
    public static NoopDisposable Instance { get; } = new();

    public void Dispose()
    {
    }
  }
}
