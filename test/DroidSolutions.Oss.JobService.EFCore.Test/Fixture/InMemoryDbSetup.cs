using System;

using Microsoft.EntityFrameworkCore;

namespace DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

public class InMemoryDbSetup : IDisposable
{
  public InMemoryDbSetup()
  {
    Context = new TestContext(new DbContextOptionsBuilder<TestContext>().UseInMemoryDatabase("TestDatabase").Options);
  }

  public TestContext Context { get; }

  public void Dispose()
  {
    Context?.Dispose();
  }
}
