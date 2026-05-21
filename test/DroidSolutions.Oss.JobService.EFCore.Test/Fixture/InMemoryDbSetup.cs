using System;

using Microsoft.EntityFrameworkCore;

namespace DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

public class InMemoryDbSetup : IDisposable
{
  public InMemoryDbSetup()
  {
    Context = new SampleContext(new DbContextOptionsBuilder<SampleContext>().UseInMemoryDatabase("TestDatabase").Options);
  }

  public SampleContext Context { get; }

  public void Dispose()
  {
    Context?.Dispose();
  }
}
