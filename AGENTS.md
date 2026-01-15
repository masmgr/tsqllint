# Agent instructions for this repository

## Overview
- TSQLLint is a .NET-based CLI linting tool that ships as a dotnet tool (`tsqllint`) and targets `net6.0;net7.0;net8.0`.
- The solution lives at `source/TSQLLint.sln` and links the console app, core logic, infrastructure helpers, and NUnit-based tests.
- Follow the README and CONTRIBUTING guidance before making interface changes; updating documentation (especially `README.md` and `documentation/rules`) and tests is required whenever behavior changes.

## Key areas
- `source/TSQLLint/`, `source/TSQLLint.Core/`, `source/TSQLLint.Infrastructure/` contain the production assemblies; `source/TSQLLint.Tests/` holds unit/functional fixtures plus SQL assets under `UnitTests/` and `FunctionalTests/`.
- `documentation/` stores user-facing assets (rule descriptions, screenshots, GIF), so keep it in sync when you describe new rules or UI behavior.
- `.github/workflows/ci.yml` runs matrix builds/tests on Ubuntu/Windows for `.NET 6/7/8`; `.github/stale.yml` auto-closes inactive issues after 90 days (7 days after marking stale) and exempts `bug`, `enhancement`, and `security` issues.
- `.circleci/config.yml` orchestrates release builds, artifact storage, and Slack notifications, referencing `scripts/dotnet_build_test.sh`, `scripts/dotnet_package.sh`, and `scripts/github_create_release.sh`.

## Building & testing
- Restore: `dotnet restore source/TSQLLint.sln --verbosity minimal` (CI caches `~/.nuget/packages` via `hashFiles('**/*.csproj')`).
- Build: `dotnet build source/TSQLLint.sln --configuration Release --no-restore`.
- Test: `dotnet test source/TSQLLint.sln --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" --settings source/coverlet.runsettings --results-directory ./coverage`. CI expects this command to emit coverage artifacts; the GH action additionally generates HTML reports on Ubuntu + .NET 8.
- Code analysis: StyleCop analyzers are enabled in each project (`source/.stylecop/stylecop.ruleset`); warnings are treated as errors in `Debug|AnyCPU`.
- Use `scripts/dotnet_build_test.sh <tfm>` (inside Docker) to mimic CI, or `scripts/dotnet_build_test_local.sh` to launch that script from a container. Both rely on `scripts/utils.sh` to set environment/version variables.

## Scripts & releases
- `scripts/dotnet_package.sh` packs `source/TSQLLint.sln`, publishes assets for multiple runtimes (`linux-*`, `win-*`, `osx-*`), then pushes the NuGet package when `RELEASE=true` and `NUGET_API_KEY` is set.
- `scripts/github_create_release.sh` uploads the archived builds to a GitHub release via `gh` (needs `GITHUB_TOKEN_FILE`).
- `scripts/github_create_release.sh`, `scripts/dotnet_package.sh`, and `scripts/dotnet_build_test.sh` all source `scripts/utils.sh`; that helper enforces Docker execution, derives versions from tags/branches, creates `artifacts/` and `packages/`, and exposes the usual env vars (`BRANCH_NAME`, `RELEASE`, `VERSION`, etc.).

## Conventions & reminders
- Keep edits ASCII unless the file already uses non-ASCII text.
- Dotnet CLI code generally uses StyleCop formatting, `appsettings.json` is copied to output, and warnings such as `NU1701/NU1702/NU1705` are globally suppressed.
- Functional/unit tests may rely on shipped SQL files; leave those in place and add new fixtures where necessary.
- Follow the code of conduct described in `CODE_OF_CONDUCT.md`; refer to `CONTRIBUTING.MD` before contributing and target pull requests at the `develop` branch (the GH action watches `main`/`develop` for PRs and pushes).
- When describing lint rule behavior or interfaces, reference the `README.md` usage section and update animation or screenshot assets in `documentation/` if the experience changes.

## Testing & verification path
- CI uses matrix: Ubuntu/Windows × .NET 6/7/8; rely on the same Release configuration locally to match results (tests use NUnit + NSubstitute + coverlet). Windows-specific behavior should be verified on a Windows runner when possible.
- Coverage reports are generated with `reportgenerator` and stored under `./coverage/report`; CI uploads `coverage/report/` as artifacts only from Ubuntu + .NET 8.
- The CircleCI release workflow runs `scripts/dotnet_build_test.sh net8.0`, archives artifacts under `artifacts/`, and later packages, publishes, and notifies Slack via `circleci/slack`.

## Additional notes
- The project still supports the old Homebrew/dotnet tool/NPM install paths described in the README; preserving that compatibility is why `TSQLLint` is packaged as a tool.
- Keep `documentation/rules` synchronized with actual rule implementations in `source/TSQLLint.Core`.
