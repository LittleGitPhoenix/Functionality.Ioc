# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
___

## 2.3.0

:calendar: _2023-09-09_

### Added

- New `TypeAndFactoryList{T}` that can be used to obtain the types of registered services (as opposed to their instances) alongside a factory for actually creating instances of the type via the appropriate **IRegistrationSource** `TypeAndFactoryListSource{T}`.

### Fixed

- New `RegisterFactory` extension method to now checks if the _delegate factory_ that should get registered is declared within another type and not standalone. Additionally it is checked, whether the return type of the _delegate factory_ is assignable to the type it is declared in. Those requirements stem from **Autofac** itself, as the extension method actually registers the return type of the _delegate factory_.
___

## 2.2.0

:calendar: _2022-05-03_

### Added

- New `InternalConstructorFinder` that can be used to register types that provide internal constructors.
- New `RegisterFactory` extension method to register a type via a [delegate factory](https://docs.autofac.org/en/latest/advanced/delegate-factories.html).
___

## 2.1.0

:calendar: _2022-03-12_

### Added

- New `TypeList{T}` that can be used to obtain the types of registered services (as opposed to their instances) via the appropriate **IRegistrationSource** `TypeListSource{T}`.
___

## 2.0.0

:calendar: _2022-01-08_

### Added

- Project now natively supports **.NET 6**.

### Changed

- The `IocScopeVerificationDelegate` does no longer just return a simple string, but rather an instance of the new `IocScopeVerificationResult` entity. This is a breaking change. To keep required changes to a minimum, each `IocScopeVerificationResult` can be implicitly created from and be converted to a string.
- The **Autofac** package has been downgraded from ~~6.3.0~~ to **6.0.0** so that using this package no longer forces updates in consuming projects that may be unnecessary.

### References

:large_blue_circle: Autofac ~~6.3.0~~ → **6.0.0**
___

## 1.0.0

:calendar: _2021-11-27_

Initial release.