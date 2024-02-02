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
