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
