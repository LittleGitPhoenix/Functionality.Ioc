# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
___

## 2.1.0 (2022-03-12)

### Added

- New `TypeList{T}` that can be used to obtain the types of registered services (as opposed to their instances) via the appropriate **IRegistrationSource** `TypeListSource{T}`.
___

## 2.0.0 (2022-01-08)

### Added

- Project now natively supports **.NET 6**.

### Changed

- The `IocScopeVerificationDelegate` does no longer just return a simple string, but rather an instance of the new `IocScopeVerificationResult` entity. This is a breaking change. To keep required changes to a minimum, each `IocScopeVerificationResult` can be implicitly created from and be converted to a string.
- The **Autofac** package has been downgraded from ~~6.3.0~~ to **6.0.0** so that using this package no longer forces updates in consuming projects that may be unnecessary.

### References

:large_blue_circle: Autofac ~~6.3.0~~ → **6.0.0**
___

## 1.0.0 (2021-11-27)

Initial release.