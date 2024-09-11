using System.Diagnostics.CodeAnalysis;

using DroidSolutions.Oss.JobService.EFCore;
using DroidSolutions.Oss.JobService.EFCore.Entity;

using Microsoft.EntityFrameworkCore;

namespace DroidSolutions.Oss.JobService.Postgres.Test.Fixture;

public class TestContext : DbContext, IJobContext
{
  public TestContext([NotNull] DbContextOptions options)
    : base(options)
  {
  }

  public DbSet<JobBase> Jobs { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Job<TestParameter, TestResult>>();
  }
}
