# [4.0.0-develop.1](https://github.com/droidsolutions/job-service/compare/v3.6.0...v4.0.0-develop.1) (2025-04-17)


### Features

* allow async calls in worker Hooks ([c86570e](https://github.com/droidsolutions/job-service/commit/c86570e8a3fe502ace684b6e75ce9654b6eeac7c))
* upgrade project to .NET 9.0 ([c09f34c](https://github.com/droidsolutions/job-service/commit/c09f34c18693ee0a66b6372fb93aaf8315fff4d8))


### BREAKING CHANGES

* (.NET) Pre- and PostJobRunHook are now async, take a CancellationToken and return a ValueTask
and are also renamed to PreJobRunHookAsync and PostJobRunHookAsync
* (NodeJS) pre- and postJobRunHook are now async, take an AbortSignal and return a Promise<void>
and are also renamed to preJobRunHookAsync and postJobRunHookAsync

# [3.6.0](https://github.com/droidsolutions/job-service/compare/v3.5.0...v3.6.0) (2024-11-28)


### Features

* **NodeJS:** allow to override getJobAsync by making it protected ([6393be9](https://github.com/droidsolutions/job-service/commit/6393be9613636bad874ecefa3be9efe2a2dd5f17))

# [3.5.0](https://github.com/droidsolutions/job-service/compare/v3.4.0...v3.5.0) (2024-09-18)


### Features

* add AddNextJobIn method to worker ([98ccfab](https://github.com/droidsolutions/job-service/commit/98ccfabc898591e9d33e2e46d0e126a002641eea))
* **NodeJS:** add TimeSPan custom type to mirror .NET TimeSpan ([291660c](https://github.com/droidsolutions/job-service/commit/291660cca1d8419b1e9b3d5b225af85dcaacae19))

# [3.4.0](https://github.com/droidsolutions/job-service/compare/v3.3.3...v3.4.0) (2024-09-11)


### Features

* add LastJobExecutionStart and Stop to JobWorkerBase ([53e8832](https://github.com/droidsolutions/job-service/commit/53e88327e0b1eb17018bb80a9989e8dea11d79db))

## [3.3.3](https://github.com/droidsolutions/job-service/compare/v3.3.2...v3.3.3) (2024-05-23)


### Bug Fixes

* **NodeJS:** use UTC date when adding new job ([6bf43d9](https://github.com/droidsolutions/job-service/commit/6bf43d9fbfc5ca46441a31bfc4b37ad44deb2ded))

## [3.3.2](https://github.com/droidsolutions/job-service/compare/v3.3.1...v3.3.2) (2024-02-21)


### Bug Fixes

* **JobWorkerBase:** break worker loop when exception is thrown ([7efa16a](https://github.com/droidsolutions/job-service/commit/7efa16a9115fc0861611b4e1e865ef20d296cacb))
* **JobWorkerBase:** improve exception handling when cancelled ([45385a5](https://github.com/droidsolutions/job-service/commit/45385a57e8240641fdfeb0de1198b36231a80ac6))

## [3.3.1](https://github.com/droidsolutions/job-service/compare/v3.3.0...v3.3.1) (2024-02-16)


### Bug Fixes

* **JobWorkerBase:** remove duplicate metric prefix ([8ac0b9c](https://github.com/droidsolutions/job-service/commit/8ac0b9c6c9436028fadcb43215b1f6d50beb2abf))

# [3.3.0](https://github.com/droidsolutions/job-service/compare/v3.2.3...v3.3.0) (2024-02-14)


### Features

* **JobWorkerBase:** add .NET Meter metrics for job count and duration ([d58a378](https://github.com/droidsolutions/job-service/commit/d58a37812c9e89fec06d9334a6bbd6c34f0d7858))

## [3.2.3](https://github.com/droidsolutions/job-service/compare/v3.2.2...v3.2.3) (2024-02-12)


### Bug Fixes

* **NodeJS:** add jobType to logs when jobs are deleted ([1429243](https://github.com/droidsolutions/job-service/commit/142924344b977d320b7f4794278bb6f316ec204b))

## [3.2.2](https://github.com/droidsolutions/job-service/compare/v3.2.1...v3.2.2) (2024-02-12)


### Bug Fixes

* **JobWorkerBase:** only run delete jobs once every 24 hours ([263385a](https://github.com/droidsolutions/job-service/commit/263385a068908ca9a852f41951e10e4f8ae34ed5))
* **NodeJS:** really only run delete jobs once every 24 hours ([b43cab6](https://github.com/droidsolutions/job-service/commit/b43cab604b9cdcff1e02dfeb1332e6ee5c905019))

## [3.2.1](https://github.com/droidsolutions/job-service/compare/v3.2.0...v3.2.1) (2024-02-12)


### Bug Fixes

* **NodeJS:** only run delete jobs once every 24 hours ([ecb22dd](https://github.com/droidsolutions/job-service/commit/ecb22ddd5755b804702df0f918c4f35b45dd9d97))

# [3.2.0](https://github.com/droidsolutions/job-service/compare/v3.1.0...v3.2.0) (2024-02-06)


### Features

* **JobWorkerBase:** optional remove old jobs after job run ([661658e](https://github.com/droidsolutions/job-service/commit/661658ef7e43e5cc60c48ac4c9e2a4efcd230dc3))

# [3.1.0](https://github.com/droidsolutions/job-service/compare/v3.0.1...v3.1.0) (2024-02-02)


### Bug Fixes

* **Net:** unify JobId and JobType in logs ([2143939](https://github.com/droidsolutions/job-service/commit/2143939e9b44e88328643e4634fcf3511f286ebc))


### Features

* **Net:** Update to .NET 8 ([056f8d9](https://github.com/droidsolutions/job-service/commit/056f8d997699a2b60b020b7cbad088ca5080b6ba))

## [3.0.1](https://github.com/droidsolutions/job-service/compare/v3.0.0...v3.0.1) (2023-09-12)


### Bug Fixes

* **NodeJS:** use new Date for initial jobs ([77aa178](https://github.com/droidsolutions/job-service/commit/77aa1784b61806b73548f6e38a69edb7df4ce199))

# [3.0.0](https://github.com/droidsolutions/job-service/compare/v2.1.2...v3.0.0) (2023-08-08)


### Bug Fixes

* **deps:** Update NanoID to v3 ([eb77ee6](https://github.com/droidsolutions/job-service/commit/eb77ee62b996ae484309d696a1e57998888141a5))
* **net:** Update NanoId to 3.0.0 ([f17ac88](https://github.com/droidsolutions/job-service/commit/f17ac88536452050c6b5a3f7f0db54b9ea6419da))


### Features

* **Node:** add IJobBase to export ([fafbc75](https://github.com/droidsolutions/job-service/commit/fafbc75b56beb95ea45d53580c0cf543542dd3f3))
* **NodeJS:** replace cancellationtoken with AbortSignal ([dce496d](https://github.com/droidsolutions/job-service/commit/dce496dc1c4fdeb83159b1125bd0cde7e5570bd1))


### BREAKING CHANGES

* **net:** NanoID 3.0.0 comes with changed namespace which can lead to problems when using
another version along with this package.
* **NodeJS:** JobWorkerBase.executeAsync, processJobAsync and IJobRepository methods now receive
an AbortSignal which is native to NodeJS since 15 instead of the cancellationtoken.
Remove cancellationToken peer dependency, in favor of NodeJS AbortSignal
* **NodeJS:** removed isCancellationError function

# [3.0.0-develop.2](https://github.com/droidsolutions/job-service/compare/v3.0.0-develop.1...v3.0.0-develop.2) (2023-08-08)


### Bug Fixes

* **net:** Update NanoId to 3.0.0 ([f17ac88](https://github.com/droidsolutions/job-service/commit/f17ac88536452050c6b5a3f7f0db54b9ea6419da))


### Features

* **Node:** add IJobBase to export ([fafbc75](https://github.com/droidsolutions/job-service/commit/fafbc75b56beb95ea45d53580c0cf543542dd3f3))


### BREAKING CHANGES

* **net:** NanoID 3.0.0 comes with changed namespace which can lead to problems when using
another version along with this package.

# [3.0.0-develop.1](https://github.com/droidsolutions/job-service/compare/v2.1.2...v3.0.0-develop.1) (2023-08-04)


### Bug Fixes

* **deps:** Update NanoID to v3 ([eb77ee6](https://github.com/droidsolutions/job-service/commit/eb77ee62b996ae484309d696a1e57998888141a5))


### Features

* **NodeJS:** replace cancellationtoken with AbortSignal ([dce496d](https://github.com/droidsolutions/job-service/commit/dce496dc1c4fdeb83159b1125bd0cde7e5570bd1))


### BREAKING CHANGES

* **NodeJS:** JobWorkerBase.executeAsync, processJobAsync and IJobRepository methods now receive
an AbortSignal which is native to NodeJS since 15 instead of the cancellationtoken.
Remove cancellationToken peer dependency, in favor of NodeJS AbortSignal
* **NodeJS:** removed isCancellationError function

## [2.1.2](https://github.com/droidsolutions/job-service/compare/v2.1.1...v2.1.2) (2023-04-05)


### Bug Fixes

* **NET:** republish to NuGet.org ([8dfbdb5](https://github.com/droidsolutions/job-service/commit/8dfbdb56cc5550e02117c8e47372caf2f84cf026))

## [2.1.1](https://github.com/droidsolutions/job-service/compare/v2.1.0...v2.1.1) (2023-04-05)


### Bug Fixes

* **NET:** republish for NuGet registry ([ef5151e](https://github.com/droidsolutions/job-service/commit/ef5151e5be740d449b30716c7082f68a826e029b))

# [2.1.0](https://github.com/droidsolutions/job-service/compare/v2.0.0...v2.1.0) (2023-04-05)


### Features

* update to .NET 7 ([ca8e5c6](https://github.com/droidsolutions/job-service/commit/ca8e5c6431032db3780c76170ef3a7605e966789))

# [2.0.0](https://github.com/droidsolutions/job-service/compare/v1.1.0...v2.0.0) (2023-02-21)


### Bug Fixes

* **PostgresJobRepo:** find table by base entity ([849f09a](https://github.com/droidsolutions/job-service/commit/849f09a52788e6e8323d48d0b982f37cb8109f58))
* **PostgresJobRepository:** add order by to prevent EF Core warning ([d200309](https://github.com/droidsolutions/job-service/commit/d2003090eedb60bfe431719040057f7365b62860))
* prevent add initial job if another is running ([e4b607a](https://github.com/droidsolutions/job-service/commit/e4b607a00881bacc3f68749f2548b06ad71d6eb5))


### Features

* add non generic IJobBase and JobBase ([2472960](https://github.com/droidsolutions/job-service/commit/24729609d5bdf6c310f0ca656d78bd6884fd80e0))
* **JobWorkerBase:** add more run metrics ([38c8076](https://github.com/droidsolutions/job-service/commit/38c8076af585955fccc0ff7ebb2187a7884e1f9b))
* **TS:** export and use IJobWorkerMetrics ([aa49220](https://github.com/droidsolutions/job-service/commit/aa4922083a449d2c9d150ba5b5c036bfb3749f2e))


### BREAKING CHANGES

* IJobRepository.FindExistingJobAsync now has an additional parameter includeStarted
to also find a job that is started.

Fixes a bug where a starting worker would add an initial job while another running worker is
executing the same job.

# [2.0.0-develop.6](https://github.com/droidsolutions/job-service/compare/v2.0.0-develop.5...v2.0.0-develop.6) (2023-02-16)


### Bug Fixes

* **PostgresJobRepository:** add order by to prevent EF Core warning ([d200309](https://github.com/droidsolutions/job-service/commit/d2003090eedb60bfe431719040057f7365b62860))

# [2.0.0-develop.5](https://github.com/droidsolutions/job-service/compare/v2.0.0-develop.4...v2.0.0-develop.5) (2023-01-31)


### Features

* **JobWorkerBase:** add more run metrics ([38c8076](https://github.com/droidsolutions/job-service/commit/38c8076af585955fccc0ff7ebb2187a7884e1f9b))
* **TS:** export and use IJobWorkerMetrics ([aa49220](https://github.com/droidsolutions/job-service/commit/aa4922083a449d2c9d150ba5b5c036bfb3749f2e))

# [2.0.0-develop.4](https://github.com/droidsolutions/job-service/compare/v2.0.0-develop.3...v2.0.0-develop.4) (2023-01-26)


### Bug Fixes

* **PostgresJobRepo:** find table by base entity ([849f09a](https://github.com/droidsolutions/job-service/commit/849f09a52788e6e8323d48d0b982f37cb8109f58))

# [2.0.0-develop.3](https://github.com/droidsolutions/job-service/compare/v2.0.0-develop.2...v2.0.0-develop.3) (2023-01-26)


### Features

* add non generic IJobBase and JobBase ([2472960](https://github.com/droidsolutions/job-service/commit/24729609d5bdf6c310f0ca656d78bd6884fd80e0))

# [2.0.0-develop.2](https://github.com/droidsolutions/job-service/compare/v2.0.0-develop.1...v2.0.0-develop.2) (2022-11-03)


### Features

* **JobRepository:** add method to count jobs ([05f6475](https://github.com/droidsolutions/job-service/commit/05f64757f874c76ca2a92fa8e1556147c55d2a63))

# [2.0.0-develop.1](https://github.com/droidsolutions/job-service/compare/v1.0.1...v2.0.0-develop.1) (2022-07-05)


### Bug Fixes

* prevent add initial job if another is running ([e4b607a](https://github.com/droidsolutions/job-service/commit/e4b607a00881bacc3f68749f2548b06ad71d6eb5))


### BREAKING CHANGES

* IJobRepository.FindExistingJobAsync now has an additional parameter includeStarted
to also find a job that is started.

Fixes a bug where a starting worker would add an initial job while another running worker is
executing the same job.

# [1.1.0](https://github.com/droidsolutions/job-service/compare/v1.0.1...v1.1.0) (2022-11-03)


### Features

* **JobRepository:** add method to count jobs ([05f6475](https://github.com/droidsolutions/job-service/commit/05f64757f874c76ca2a92fa8e1556147c55d2a63))

## [1.0.1](https://github.com/droidsolutions/job-service/compare/v1.0.0...v1.0.1) (2022-05-11)


### Bug Fixes

* real Initial release ([6388a50](https://github.com/droidsolutions/job-service/commit/6388a50784833d11b36df138be7749b1b70fb885))

# 1.0.0 (2022-05-11)


### Bug Fixes

* **JobWorkerBase:** respect calculated duedate when adding initial job ([4497723](https://github.com/droidsolutions/job-service/commit/449772344c061fd7747dfba23f0d7797ba33a456))
* **NPM:** add export for IJobRepository ([6d499d6](https://github.com/droidsolutions/job-service/commit/6d499d6f4ce977bcdffbc01123acbba08092a72d))
* **NPM:** add missing files to package ([7ec3571](https://github.com/droidsolutions/job-service/commit/7ec3571b5621d1b28d231eb208b38a33d02f015d))
* **release:** correctly set version during release ([a00c8fe](https://github.com/droidsolutions/job-service/commit/a00c8fe60a7e00a0019db826314e66152397b5e7))
* **release:** minor fix to trigger new release ([011e47a](https://github.com/droidsolutions/job-service/commit/011e47a4abac0d7af99d8b32ae3243ff6aba000d))


### Features

* initial release ([d71d5b3](https://github.com/droidsolutions/job-service/commit/d71d5b3704ef13af82efaa8d2cf1588d45a55048))
* **NPM:** add UTC converter to exports ([2ae9e69](https://github.com/droidsolutions/job-service/commit/2ae9e69860e08efc144f85910475948dda5d2683))
* **NPM:** export LoggerFactgory and camcellationError typeguard ([87ff1b5](https://github.com/droidsolutions/job-service/commit/87ff1b5394c7efe937233bdecb78ee99b3524092))

# [1.0.0-develop.8](https://github.com/droidsolutions/job-service/compare/v1.0.0-develop.7...v1.0.0-develop.8) (2022-04-21)


### Features

* **NPM:** add UTC converter to exports ([2ae9e69](https://github.com/droidsolutions/job-service/commit/2ae9e69860e08efc144f85910475948dda5d2683))

# [1.0.0-develop.7](https://github.com/droidsolutions/job-service/compare/v1.0.0-develop.6...v1.0.0-develop.7) (2022-04-19)


### Features

* **NPM:** export LoggerFactgory and camcellationError typeguard ([87ff1b5](https://github.com/droidsolutions/job-service/commit/87ff1b5394c7efe937233bdecb78ee99b3524092))

# [1.0.0-develop.6](https://github.com/droidsolutions/job-service/compare/v1.0.0-develop.5...v1.0.0-develop.6) (2022-04-19)


### Bug Fixes

* **NPM:** add export for IJobRepository ([6d499d6](https://github.com/droidsolutions/job-service/commit/6d499d6f4ce977bcdffbc01123acbba08092a72d))

# [1.0.0-develop.5](https://github.com/droidsolutions/job-service/compare/v1.0.0-develop.4...v1.0.0-develop.5) (2022-04-19)


### Bug Fixes

* **NPM:** add missing files to package ([7ec3571](https://github.com/droidsolutions/job-service/commit/7ec3571b5621d1b28d231eb208b38a33d02f015d))

# [1.0.0-develop.4](https://github.com/droidsolutions/job-service/compare/v1.0.0-develop.3...v1.0.0-develop.4) (2022-04-19)


### Bug Fixes

* **JobWorkerBase:** respect calculated duedate when adding initial job ([4497723](https://github.com/droidsolutions/job-service/commit/449772344c061fd7747dfba23f0d7797ba33a456))

# [1.0.0-develop.3](https://github.com/droidsolutions/job-service/compare/v1.0.0-develop.2...v1.0.0-develop.3) (2022-03-18)


### Bug Fixes

* **release:** correctly set version during release ([a00c8fe](https://github.com/droidsolutions/job-service/commit/a00c8fe60a7e00a0019db826314e66152397b5e7))

# [1.0.0-develop.2](https://github.com/droidsolutions/job-service/compare/v1.0.0-develop.1...v1.0.0-develop.2) (2022-03-18)


### Bug Fixes

* **release:** minor fix to trigger new release ([011e47a](https://github.com/droidsolutions/job-service/commit/011e47a4abac0d7af99d8b32ae3243ff6aba000d))

# 1.0.0-develop.1 (2022-03-18)


### Features

* initial release ([d71d5b3](https://github.com/droidsolutions/job-service/commit/d71d5b3704ef13af82efaa8d2cf1588d45a55048))
