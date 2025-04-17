# DroidSolutions Job Service

Library to manage recurring jobs.

The DroidSolutions Job service offers tools that help managing recurring jobs. It is split in multiple packages that are explained below. These contains NuGet packages for .NET 9 as well as an NPM package.

Examples of jobs are:

- check every hour for new entries in a database to process
- download current information from an API every day
- run a job to clean up no longer needed data
- delay a task coming from UI to execute at a later time

## Why not Cron?

For recurring jobs you could also use a cronjob and there is nothing wrong with that. In general this library allows to have a similar experience like with a cron job with the following differences:

- Jobs are not executed exactly at a given time. Instead they have a due date and whenever a runner is checking for a job it will execute the job with the oldest due date that's already passed. Depending on the check interval you configure there might be some time between due date and actual execution. In fact, jobs do not need to be recurring at all, you can use this library to manage one time jobs.
- Job execution intervals are not always equal. When a job is executed and it is configured to add another job, the next job is added **after** the previous job is done executing. The next due date will be calculated from that point on, so time between jobs depend on the execution time and the configured interval.
- Jobs are persistent, you have a storage for all jobs (presumably a database table) that you can use to display job executions or view results of the jobs.
- Job execution can be integrated in your application. While a cronjob probably is an own piece of software or a script, the job worker can be integrated in your application and share code with it. This is useful when the job needs logic that other parts of your application also need. You also can easily dynamically add a job from your application logic if it is needed.

# Packages

This section describes which packages are availabe for which eco systems.

## DroidSolutions.Oss.JobService

This NuGet package contains interfaces for a job and a job repository as well as a base worker service to work with them.

## DroidSolutions.Oss.JobService.EFCore

This NuGet package contains a concrete implementation of the IJob and IJobRepository using [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/). It also offers an interface that a DbContext can implement that the repository implementation needs.

## DroidSolutions.Oss.JobService.Postgres

This NuGet package contains a concrete implementation of the IJobRepository with Postgres specific JSON querying using [Npgsql](https://www.npgsql.org/efcore/index.html).

## @droidsolutions-oss/job-service

This NPM package contains TypeScript interfaces that are generated from the .NET interfaces of the [DroidSolutions.Oss.JobService package](#droidsolutions-ossjob-service) and an abstract `JobWorkerBase` class. These can be used for a NodeJS implementation of the job repository and the worker service.

# Interfaces and Classes

The main interfaces and classes of the packages are described in this section.

## IJobBase

The base of this library are jobs. Jobs can be added, started, finished and processed.

This interface is available in C# as well as [TypeScript](https://www.typescriptlang.org/).

A job is meant to be stored somewhere (like a table in a database) and holds information about what it should do. This includes dates of creation, last update and when the job is due. There is also a state and a type to distinguish jobs of different types as well as optional properties that relate to job progress.

This interface describes base jobs without any parameters or result. They can be used when they don't need additional information to run or store their result elsewhere.

## IJob<TParams, TResult>

An extension of the base jobs with generic type arguments for parameters and results.

In addition to base jobs they store parameters and a result with them. There is an interface `IJob<TParams, TResult>` which serves as the basis for a concrete database entity.

This interface is available in C# as well as [TypeScript](https://www.typescriptlang.org/).

Jobs are generic, where the type arguments are meant for parameters and the result that a job can have. Those can be serialized to and from JSON when interacting with the database.

There is a concrete implementations of `IJob<TParams, TResult>` for [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) in the [DroidSolutions.Oss.JobService.EFCore package](#droidsolutionsossjobserviceefcore).

### Entity Framework Core specifics

The `JobBase` class is a concrete implementation of the `IJobBase` interface which has annotations that may be useful when using it as an entity for [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/).

The `Job<TParams, TResult>` class is an extension that implementats the `IJob<TParams, TResult>` interface. The parameters and results are currently directly mapped to `jsonb` column for [PostgreSQL](https://www.postgresql.org/docs/current/datatype-json.html).

Internally the properties for parameter and result are mapped to fields in the database to allow interoperatibility with NodeJS or other projects. This means there are `ParametersSerialized` and `ResultSerialized` properties that contain the JSON string and are mapped to the database where the `Parameters` and `Result` properties contain the actual deserialized values for you to use.

When implementing your own `IJobRepository<TParams, TResult>` you are responsible for serializing and deserializing the values yourself.

## IJobRepository<TParams, TResult>

There is a C# and a TypeScript interface for a repository that works with the job entity. It can be implemented to serve as a wrapper for database related actions. The generic type parameters are the same as for jobs, so a repository is repsonsible for exactly one type of jobs. The repository acts as the data layer of the application and wraps around all database interaction regarding jobs.

Like for the job there are concrete implementations for EF Core and TypeORM.

### Entity Framework Core specifics

On the .NET side there is an interface `IJobContext` that can be implemented in your DbContext. This uses the `JobBase` class which has annotations that may be useful when using it as an entity for [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/).

The `JobRepositoryBase<TContext, TParams, TResult>` is a full implementation of the `IJobRepository<TParams, TResult>` interface using the `Job<TParams, TResult>` entity where necessary. Parameters and results are optional, that's why the `IJobContext` only uses the `BaseJob` entity. The `JobRepositoryBase<TContext, TParams, TResult>` handles checks for parameter and result existence and uses `JobBase` where they don't apply. If they are needed, `JobBase` entities are casted to `Job<TParams, TResult>`. For this to work, your `DbContext` must register the entity, for example like this:

```cs
public class TestContext : DbContext, IJobContext
{
  public DbSet<JobBase> Jobs { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // Register entity with concrete params and result types
    modelBuilder.Entity<Job<TestParameter, TestResult>>();
  }
}
```

To allow interoperatibility with job workers that use NodeJS the parameters and result are serialized with special options using camelCase property names. This can be extended or overwritten via the protected `GetSerializerOptions` method. In the background the `Job<TParams, TResult>` class has `ParametersSerialized` and `ResultSerialized` properties that hold the converted json and act as the actual database columns. The repository handles this in all methods where it is necessary so you shouldn't have to do this yourself.

Adding jobs, starting jobs and setting job progress is done via transactions and table or row locks. This ensures no two parallel running workers can receive the same job or work on the same job.

The repository constructor expects an instance of the data context that implements `IJobContext` as well a an `ILogger` instance. If you have set up dependency injection in a standard ASP.NET Core app there should be no problem, just make sure you have added the `DbContext` that implements `IJobContext` to the depdency injection via `services.AddDbContext()`.

## Worker

The `JobWorker<TParams, TResult>` (C#) or `JobWorkerBase` (TypeScript) is an abstract base class for managing the jobs. It uses the `IJobRepository<TParams, TResult>` interface to work with the database. It checks for available jobs, calls a processing function for each and finishes the job (or resets it in case of a failure). It offers some extensibility methods where the implementer can add code specific for the type of job it wants to process.

Pass an instance of the `JobRepository` or any other class that implements `IJobRepository<TParams, TResult>` to the constructor, along with a settings object. You can use the `IJobWorkerSettings` interface in NodeJS to know which properties are expected.

There are two lifecycle hooks before and after each run that can optionally be implemented to add custom logic before and after each job run.

### ASP.NET Core specifics

Add a class for your worker that extends the `JobWorker<TParams, TResult>` class and implements the abstract methods.

The extended worker class can be added as a [hosted service](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services). Be sure to configure the dependency injection to be able to provide an OptionsMonitor with the `JobWorkerSettings` to the worker constructor. For example you could use something like this:

```cs
services.Configure<JobWorkerSettings>(Configuration.GetSection("WorkerSettings"));
```

You can also extend the settings class if you need other settings for your implemented worker.

The `PreJobRunHook` is called when a new run is executed. At this time the worker has not checked if a job is available and therefore no job exists yet. This hook can be used to set up a logger, correlation id, Sentry transactions or a general health check, though it is not guarenteed that a job is available.

**Note** The `JobWorkerBase` uses the [NanoId](https://github.com/codeyu/nanoid-net) package. If you don't use the NanoId package yourself, this should not be a problem, but if you use a different version of NanoId there can be problems.

### NodeJS specifics

The TypeScript side has an abstract `JobWorkerBase` class that can be used as the base of a worker implementation. For this you'll need a concrete implementation of the IJobRepository interface and pass it along with settings to the constructor. You'll then have to implement the abstract methods and you'll should be good to go.

# Usage

The following is a detailed guide on how to use this library for either ASP.NET Core (C#) or NodeJS (TypeScript).

## ASP.NET Core

The concrete implementation depends on the kind of database you want to use. There are already implementations for Entity Framework Core and PostgreSQL for which the following guide is specific to.

1. Add the `DroidSolutions.Oss.JobService`, `DroidSolutions.Oss.JobService.EFCore` and `DroidSolutions.Oss.JobService.Postgres` packages as well as references to `Microsoft.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Create types for your job parameters and result. If you don't need one or both of them you can use `object?`.
3. Let your db context implement the `IJobContext` interface. That could look like this

   ```cs
   public class SampleContext : DbContext, IJobContext
   {
     public SampleContext([NotNull] DbContextOptions options)
       : base(options)
     {
     }

     public DbSet<JobBase> Jobs { get; set; }
   }
   ```

4. Register the job entities you need in the `DbContext`, if you don't need parameters and or results, use `object`:

   ```cs
   public class SampleContext : DbContext, IJobContext
   {
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        // Register entity with object for jobs without params and result
        modelBuilder.Entity<Job<object, object>>();
        // or register entity with concrete params and result types
        modelBuilder.Entity<Job<SampleParameter, SampleResult>>();
      }
   }
   ```

5. If you want to share the database with services in other languages (such as NodeJS) or generally want to have the `JobState` enum as a text column you can use the `JobStateToDescriptionConverter` by adding it in the `OnConfiguring` method of the db context.

   ```cs
   using DroidSolutions.Oss.JobService.EFCore.Entity;
   using DroidSolutions.Oss.JobService.EFCore.Converter;
   // ...
     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
     {
       modelBuilder.Entity<Job<SampleParameter, SampleResult>>(entity =>
       {
         entity.Property(x => x.State).HasConversion(new JobStateToDescriptionConverter());
       });
     }
   ```

6. Register the `PostgresJobRepository` class in the dependency injection. For example

   ```cs
   services.AddScoped<IJobRepository<SampleParameter, SampleResult>, PostgresJobRepository<SampleContext, SampleParameter, SampleResult>>();
   ```

   Don't forget to also register the `DbContext`.

7. Add your own worker settings class extending from `JobWorkerSettings`. This contains settings that control how your worker behaves.

   ```cs
   public class SampleJobSettings : JobWorkerSettings
   {
     public SampleJobSettings()
       : base("sampleworker:samplejob")
     {
     }

     public int MySpecialSetting { get; set; }
   }
   ```

8. Add a class that extends and implements `JobWorkerBase<TParams, TResult>`. Inject an `IOptionsMonitor<SampleJobSettings>` (or the default `JobWorkerSettings`), an `ILoggerFactory` and an instance of the IServiceProvider. Pass all three of those to the base contructor and implement the abstract methods. The result could look like this:

   ```cs
   public class SampleWorker : JobWorkerBase<SampleParameter, SampleResult>
   {
     public SampleWorker(
       IOptionsMonitor<SampleJobSettings> settings,
       IServiceProvider serviceProvider,
       ILoggerFactory loggerFactory)
       : base(settings, serviceProvider, loggerFactory.CreateLogger<JobWorkerBase<SampleParameter, SampleResult>>())
     {
     }

     protected override string GetRunnerName()
     {
       string version = typeof(Program).Assembly
         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
         .InformationalVersion

       return $"my-app-name-{version}";
     }

     // optional
     protected override SampleParameter? GetInitialJobParameters()
     {
       return new SampleParameter { SomeProp = "MyValue" };
     }

     // optional
     protected override void PreJobRunHook()
     {
       // optional logic before a job run starts
     }

     // optional
     protected override void PostJobRunHook()
     {
       // optional logic after a job is complete
     }

     /// <inheritdoc/>
     protected override async Task<SampleResult> ProcessJobAsync(
       IJob<SampleParams, SampleResult> job,
       IServiceScope serviceScope,
       CancellationToken cancellationToken)
     {
       var myService = serviceScope.ServiceProvider.GetService<MyService>();

       var result = await myService.DoSomething(job.Parameters, cancellationToken);

       return new SampleResult
       {
         MyProp = result.Something,
       };
     }
   }
   ```

   - The `GetRunnerName` method should return a string that is unique for this worker class. The base implementation will add a random string to it so it is distinguishable if your application is running in more than one instance.
   - `GetInitialJobParameters` is called when you set `AddInitialJob` in your settings to true. This way the worker will call this method to get the parameters of the first job.
   - The `PreJobRunHook` is called once before the worker checks if a job is available and can be used for custom logic. For example you could generate a correlation id and set it to the log context for following logs.
   - The `PostJobRunHook` is called once after a job run and can be used for custom cleanup logic. For example you could remove a previously set correlation id from the log context. The post hook is also called when no job was actually executed.
   - The `ProcessJobAsync` method is where you put your logic to actually process the job. It is only called when a job to execute exists and contains the job, a service scope for this execution and a CancellationToken. If your job has a result you should return it from this method.

9. Add your worker as a hosted service.

   ```cs
   services.AddHostedService<SampleWorker>();
   ```

10. Generate Migration to add or update the Job table.

## NodeJS

The worker service can be used in JavaScript/TypeScript projects.

1. Install the package

   ```bash
   npm install @droidsolutions-oss/job-service
   ```

2. Create an implementation of the `IJobRepository<TParams, TResult>` interface. If you are using TypeORM you can use the `@droidsolutions-oss/job-service-typeorm` package which already comes with an implemantation of the interface. Otherwise you can implement it with the database layer of your choice. In TypeScript it would look like this (note [Prisma](prisma.io) is used in this example):

   ```ts
   import { IJob, IJobRepository, JobState } from "@droidsolutions-oss/job-service";
   import { Prisma, PrismaClient } from "@prisma/client";

   // Note: if you only need one type of job you can also use the type directly instead of a generic here
   export class JobRepository<TParams, TResult> implements IJobRepository<TParams, TResult> {
     constructor(private readonly client: PrismaClient) {}

     public async addJobAsync(
       type: string,
       dueDate?: Date,
       parameters?: TParams,
       cancellationToken?: AbortSignal,
     ): Promise<IJob<TParams, TResult>> {
       cancellationToken?.throwIfAborted();

       // create a new job
       const job = await this.client.job.create({
         data: {
           dueDate: dueDate ?? new Date(),
           parameters: parameters as Prisma.JsonObject,
           state: JobState.Requested,
           type,
         },
       });

       // save the job to the database, reload it and then return it with the new id

       return job;
     }

     // Implement the other methods from the interface
   }
   ```

3. Optional if you need custom settings create your own settings interface that extends from `IJobWorkerSettings`.

   ```ts
   import { IJobWorkerSettings } from "@droidsolutions-oss/job-service";

   export interface MyWorkerSettings extends IJobWorkerSettings {
     checkInput: boolean;
   }
   ```

4. Create your worker, extending from `JobWorkerBase<TParams, TResult>`.

   ```ts
   import { IJob, JobWorkerBase, LoggerFactory } from "@droidsolutions-oss/job-service";
   import { JobRepository } from "../repository/JobRepository";
   import { MyWorkerSettings } from "../dto/MyWorkerSettings";

   export interface ExampleResult {
     errors?: string[];
   }

   export class ExampleWorker extends JobWorkerBase<string[], ExampleResult> {
     constructor(
       settings: MyWorkerSettings,
       jobRepo: JobRepository<string[], ExampleResult>,
       loggerFactory?: LoggerFactory,
       private readonly appVersion: string,
     ) {
       super(settings, jobRepo, loggerFactory);
     }

     public getRunnerName(): string {
      // Use host name and the current application version, JobWorkerBase will append a random string to it
       return `${process.env.HOSTNAME}-v${this.appVersion}`;
     }

     public getInitialJobParameters(): string[] | undefined {
       return undefined;
     }

     public preJobRunHook(): void {
       // Optional: run any things you want before each job run.
       // This will be executed before checking if there is a job to execute
     }

     public postJobRunHook(): void {
       // Optional: run any things for cleanup
       // This will be executed after each job, even if there was no job to execute
     }

     public async processJobAsync(job: IJob<string[], ExampleResult>, cancellationToken: AbortSignal): Promise<ExampleResult> {
       // You can safely throw here, it will be catched by JobWorkerBase
       cancellationToken.throwIfAborted();
       const result: ExampleResult = {
         errors: [];
       }

       for (const input of job.parameters)
       {
         if (cancellationToken.aborted) {
           break;
         }

         try {
           // Do you job handling here
         } catch (err) {
           result.errors.push[`Error handling input ${input}: err.message`];
         }
       }

       return result;
     }
   }
   ```

5. Let your application initialize the worker.

   ```ts
   import { Prisma, PrismaClient } from "@prisma/client";
   import { MyWorkerSettings } from "./dto/MyWorkerSettings";
   import { JobRepository } from "./repository/JobRepository";
   import { ExampleResult, ExampleWorker } from "./worker/ExampleWorker";

   const prismaClient = new PrismaClient();
   const exampleRepo = new JobRepository<string[], ExampleResult>>(prismaClient);
   const settings: MyWorkerSettings = {
     addInitialJob: true, // Create a job if none exists
     addNextJobAfter: { hours: 1 }, // Repeat job every hour
     jobType: "my-example-job",
     checkInput: true
   };
   const abortController = new AbortController();

   const worker = new ExampleWorker(settings, exampleRepo, undefined); // Optional provide a logger factory
   const executePromise = worker.executeAsync(abortController.signal);

   // When you want to stop the worker (e.g. shutting down the app) use the controller to abort
   abortController.abort(new Error("App is shutting down"));
   await executePromise; // Promise will resolve once the worker finished processing the current job or reset it if cancellationToken.throwIfAborted(); was used
   ```

# Worker

The worker service is a kind of background service that regularily checks if a job should be executed and executes the job with the earliest due date. It can also add a new job after execution thus creating end endless reoccuring job.

The worker is controlled via settings. You can create your own settings class that must extend from `JobWorkerSettings` and add your own properties that you need for job processing. But you can also just just the provided settings if you don't need your own. Those settings are explained below.

## JobWorkerSettings

`JobWorkerSettings` control how to worker behaves.

### JobType

(Required)

The most important one is the type. This string controls which type of job the worker processes. Only jobs with the exact same type are fetched and executed by the worker.

You can provide the job type in the constructor when instantiating your settings instance or set the `JobType` property.

### InitialDelaySeconds

(Optional, default `30`)

This settings controls the initial delay before the worker will start. This is used to prevent the first job run before the rest of the application is finished starting, due to the way ASP.NET Core handles `BackgroundService`s. The worker will wait the given amount of seconds before looking for the first job.

### JobPollingIntervalSeconds

(Optional, default `10`)

This settings controls the time between job processings. Once the [InitialDelaySeconds](#initialdelayseconds) have passed the worker will begin looking for the job of the type specified in the [JobType](#jobtype) that has the oldest due date. After the job is processed or if no job exists the worker will wait the amount of seconds given in this setting until it checks for a job again.

### AddNextJobAfter

(Optional, default `null`)

If a `TimeSpan` is given to this setting then the worker will add a new job after it finishes processing one and set the due date to the current date plus the time span. With this you can create an endless job that is executed every given time span.

Leave this empty if you want a one time job (or create jobs from somewhere else).

**Note:** You can specify `TimeSpan` values via `appsettings.json` by following [Standard TimeSpan format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings).

The `JobWorkerBase` has a protected method `AddNextJobIn` that receives the settings instance and the job result. It can be overridden to customize if a new job should be added based on the result of a job. For example you can create a new job if something didn't work out and you want to create a new job to retry the action.

### AddInitialJob

(Optional, default `false`)

If given the worker will look for a job of the given type with a due date in the past. If none is found the worker creates one and calls the `GetInitialJobParameters` method to get the parameters of it. The due date of the initial job will be the current date plus the [AddNextJobAfter](#addnextjobafter) period of time.

### DeleteJobsOlderThan

(Optional, default `null`)

If a `TimeSpan` is given to this setting then the worker will delete finished jobs after each job run. From the current timestamp the given timespan is substracted, every finished job of the type that is set via `JobType` whose `UpdatedAt` is older than the calculated time is removed from the database. You can use this to prevent an ever growing database, especially when you run jobs in short intervalls.

The worker will delete jobs the first time it runs and than every 24 hours. The cleaning will be between `PreJobRunHook` and `PostJobRunHook` after the current job was executed.

**Note:** You can specify `TimeSpan` values via `appsettings.json` by following [Standard TimeSpan format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings).

In `NodeJS` you can give an object with `days`, `hours`, `minutes`, `seconds` or a combination of those. For example, to remove jobs older than 6 months you could set this in you `config.json`:

```json
{
  "someWorker": {
    "jobType": "some-job",
    "deleteJobsOlderThan": {
      "days": 180
    }
  }
}
```

## Metrics

The job worker currently implements two metrics using the [.NET metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics). The static Meter is protected, so your worker can use it to add its own metrics to it.

For example you can create a counter and then add to it:

```cs
public class MyWorker : JobWorkerBase<void, void>
{
  private static readonly Counter<int> MyCounter = WorkerMeter.CreateCounter<int>("my_counter");

  // ...
  protected override async Task<TestResult> ProcessJobAsync(
    IJob<void, void> job,
    IServiceScope serviceScope,
    CancellationToken cancellationToken)
  {
    // process job, then increment counter for some objects in your job
    MyCounter.Add(1);
  }
}
```

## Health

Implementing proper health checking of your job worker is up to you, however there are some metrics provided by the `JobWorkerBase` that can help you determining if the worker is healthy.

The protected properties `LastJobExecutionStart` and `LastJobExecutionStop` (`lastJobExecutionStart` and `lastJobExecutionStop` in NodeJS) are set each time the main execution loop starts and ends. This can be used (along with `JobPollingIntervalSeconds` from worker settings) to calculate if the loop is still beeing executed.

You can also retrieve basic worker metrics so you know about last time a job actually finished and how long it took to process.

# Development

## Tests

To run tests a PostgreSQL server will be needed. If you already have one you can add a appsettings file to `test/DroidSolutions.Oss.JobService.Postgres.Test/appsettings.LocalTest.json` and configure the `DataContext` connection string.

The basic setup for a PostgreSQL container would be the following (you can replace `podman` with `docker`):

```bash
podman run --name postgres -e POSTGRES_USER=integrationtest -e POSTGRES_PASSWORD=password -e POSTGRES_DB=integrationtest -d -p 5432:5432 postgres
```

Your `appsettings.LocalTest.json` file could then look like this:

```json
{
  "ConnectionStrings": {
    "DataContext": "Host=localhost;Database=integrationtest;Username=integrationtest;Password=password;Include Error Detail=true"
  }
}
```

After this you should be able to run `dotnet test` with success.