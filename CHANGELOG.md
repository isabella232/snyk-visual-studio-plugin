# Snyk Changelog

## [1.1.24]

### Fixed
 - Extension errors on VS2017.

## [1.1.23]

### Added
 - Organization description information in settings.
 
### Fixed
- Changing custom endpoint settings leads to authentication errors.

## [1.1.22]

### Fixed
- External example fixes tab control dark theme support.
- Snyk Code results partially lost for WPF projecs.

## [1.1.21]

### Changed
- Replace the word "Remediation" with "Fix" in OSS report.

## [1.1.20]

### Fixed
- Files not detected issue.

## [1.1.19]

### Fixed
- Errors when projects are nested inside solution folders.

## [1.1.18]

### Changed
- Removed manually included DLLs from VSIX package.

## [1.1.17]

### Fixed
- Selection of tree view items only working when clicking on the icon.
- Background color of unfocused selected items might blend with font color on some themes.

## [1.1.16]

### Added
- Base64 encoding for Snyk Code analysis file content payloads.

### Fixed
- The color of the text in the tree view does not match the color from VS theme.
- A problem with partially lost Snyk Code results if a single file contains multiple identical suggestions.
- Error when clicking on issues with unknown severity in the tool window.

## [1.1.15]

### Fixed
- Fixed a bug in Snyk Code where files with an underscore in the path would be ignored.
- Restore all tree items after clear search or filter.

### Changed
- Expand all scan results after completing a scan.

## [1.1.14]

### Fixed
- Run a scan for OSS and for Snyk Code asynchronously.

## [1.1.13]

### Fixed
- Error reporting and Snyk Code configuraton issues.

## [1.1.12]

### Fixed
- Display message on main panel if error occurs.
- Link to Snyk Code settings on app.snyk.io.

### Changed
- Added analysis context information for analysis requests.
- Welcome screen text and added privacy policy, term of service links.

## [1.1.11]

### Fixed
- Fixed CLI download blocking the UI longer than necessary.

### Changed
- Improved message text when Snyk Code is disabled.
- Added Snyk to the "Extensions" menu in VS2019+, and to the top menu bar in older versions.

## [1.1.10]

### Changed
- Snyk Code: add support for Single Tenant setups.

### Fixed
- Scan of solutions in which *.sln file is not in the root directory.

## [1.1.9]

### Fixed
- "Object reference not set to an instance of an object" when launching extension in Visual Studio 2022.

## [1.1.8]

### Fixed
- Clean Open Source and Snyk Code vulnerabilities cache.
- Fixed severity icons for Snyk Code issues.

## [1.1.7]

### Fixed
- Extension crash on Visual Studio 2017.
- Cache invalidation for Open Source vulnerabilities.
