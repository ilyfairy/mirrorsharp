# Changelog

## [3.0.10] - 2024-11-19

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.12.0 (preview)
- Fixed support for Microsoft.CodeAnalysis 4.11.0 (final) with help from @Sicos1977
- Fixed support for Microsoft.CodeAnalysis 4.10.0 (final) with help from @Sicos1977
- Added explicit dependency on safe version of System.Net.Http to resolve security audit warnings
- Added explicit dependency on safe version of System.Text.RegularExpressions to resolve security audit warnings

## [3.0.9] - 2024-06-01

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.11.0 (preview versions)
- Fixed support for Microsoft.CodeAnalysis 4.10.0 (preview versions)

## [3.0.8] - 2023-10-14

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.9.0 (preview versions)

## [3.0.7] - 2023-09-07

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.8.0 (preview versions)

## [3.0.6] - 2023-05-29

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.7.0 (preview versions)
- Fixed support for Microsoft.CodeAnalysis 4.6.0

## [3.0.5] - 2023-03-12

### Fixed
- Fixed autocompletion sometimes disappearing for the durration of the session when using newer Microsoft.CodeAnalysis

## [3.0.4] - 2023-01-06

### Fixed
- Fixed sorting order of diagnostic quickfix actions (C#/VB)

## [3.0.3] - 2022-12-15

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.5.0+ (preview versions)

## [3.0.2] - 2022-10-25

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.4.0+ (preview versions)

## [3.0.1] - 2022-08-13

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.4.0+ (preview versions)
- Fixed support for Microsoft.CodeAnalysis 4.3.0+
- Fixed support for .NET Framework XML documentation redirects on x64

## [3.0.0] - 2022-04-04
## [3.0.0-test-2022-04-02-1] - 2022-04-02
## [3.0.0-test-2022-04-01-1] - 2022-04-01

### Changed
- Updated minimum .NET Core target to 3.1 (.NET Standard 2.0 should still allow older targets)
- Updated dependency on System.Memory to 4.5.4
- Made WorkSession.Extensions values nullable
- Internal restructuring (Common is now split into multiple internal packages)

### Added
- Internal prototypes of new extensions (not exposed)

### Fixed
- Fixed support for Microsoft.CodeAnalysis 4.2.0+
- Fixed support for Microsoft.CodeAnalysis 4.1.0+

### Removed
- Some obsolete properties on MirrorSharpOptions are now marked Obsolete(true) and will not compile

## [2.2.8] - 2021-09-03

### Changed
- Internal change to support MirrorSharp.IL 0.1

## [2.2.7] - 2021-05-07

### Fixed
- Error when force-requesting signature help for F#

## [2.2.7-preview-2021-07-08-1] - 2021-07-08-1
## [2.2.6] - 2021-06-22
## [2.2.5] - 2021-06-21
## [2.2.4] - 2021-06-20

### Changed
- Internal refactoring (no API changes)
- Internal diagnostics to investigate specific issues

## [2.2.3] - 2021-03-04
## [2.2.2] - 2020-12-17

### Added
- Internal prototypes of new extensions (not exposed)

## [2.2.1] - 2020-09-12

### Added
- Show XML documentation in signature help.

## [2.2.0] - 2020-09-05

### Added
- Ability to add custom Analyzers, e.g: `SetupCSharp(o => o.AnalyzerReferences = o.AnalyzerReferences.Add(...))`.