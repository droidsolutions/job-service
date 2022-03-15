using System.Diagnostics.CodeAnalysis;

using DroidSolutions.Oss.JobService.EFCore;
using DroidSolutions.Oss.JobService.EFCore.Entity;

using Microsoft.EntityFrameworkCore;

namespace DroidSolutions.Oss.JobService.Postgres.Test.Fixture;

public class TestContext : DbContext, IJobContext<TestParameter, TestResult>
{
  public TestContext([NotNull] DbContextOptions options)
    : base(options)
  {
  }

  public DbSet<Job<TestParameter, TestResult>> Jobs { get; set; } = null!;
}
