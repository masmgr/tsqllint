# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TSQLLint is a .NET linting tool for T-SQL scripts that identifies and reports anti-patterns in SQL code. It supports:
- Multiple distribution methods (dotnet-tool, Homebrew, NPM)
- Custom plugins via .NET assemblies
- Rule configuration and per-file overrides
- Automatic violation fixing
- Configuration files (.tsqllintrc) and ignore lists (.tsqllintignore)

## Build and Development

### Building the Project

```bash
# Restore dependencies
dotnet restore source/TSQLLint.sln

# Build the solution (Debug)
dotnet build source/TSQLLint.sln

# Build for Release
dotnet build source/TSQLLint.sln --configuration Release
```

### Running Tests

```bash
# Run all tests
dotnet test source/TSQLLint.sln

# Run tests for a specific framework (net6.0, net7.0, or net8.0)
dotnet test source/TSQLLint.sln -property:TargetFramework=net8.0

# Run a single test class
dotnet test source/TSQLLint.sln --filter "ClassName=TestClassName"

# Run with code coverage (requires coverlet)
dotnet test source/TSQLLint.sln --collect:"XPlat Code Coverage" --settings source/coverlet.runsettings
```

### Development Build Scripts

The project includes bash scripts for local development:

```bash
# Run build and tests locally (runs in Docker container)
./scripts/dotnet_build_test_local.sh

# Packaging (creates NuGet/tool packages)
./scripts/dotnet_package.sh
```

### Linting and Code Style

The project uses:
- **StyleCop.Analyzers** for C# code style (rules in `.stylecop/stylecop.ruleset`)
- Warnings are treated as errors in Debug configuration (`TreatWarningsAsErrors`)
- Target frameworks: .NET 6.0, 7.0, and 8.0

## Project Structure

### Key Projects

- **TSQLLint** - Entry point CLI application (Program.cs, Application.cs)
- **TSQLLint.Core** - Core business logic, command-line parsing, request handling
- **TSQLLint.Infrastructure** - Implementation of rules, parsers, configuration readers, reporters
- **TSQLLint.Tests** - Unit and functional test suites (uses NUnit, NSubstitute, System.IO.Abstractions)

### Important Directory Layout

```
source/
  TSQLLint/                 # Main CLI executable
  TSQLLint.Core/           # Core interfaces and use cases
    Interfaces/            # Core contracts
    UseCases/Console/      # Command-line handling via strategies pattern
  TSQLLint.Infrastructure/ # Rule implementations and infrastructure
    Rules/                 # Individual rule visitors (extend BaseRuleVisitor)
    Parser/                # SQL parsing, visitor building, file processing
    Plugins/               # Plugin loading and execution
    Configuration/         # Config file reading, overrides, environment setup
    Reporters/             # Violation reporting (console, timing)
  TSQLLint.Tests/
    UnitTests/            # Rule-specific tests
    FunctionalTests/       # End-to-end tests with test SQL files
```

## Core Architecture

### Rule System

Rules are implemented as visitors that extend **BaseRuleVisitor** (extends TSqlFragmentVisitor from Microsoft.SqlServer.TransactSql.ScriptDom). Key concepts:

- **TSqlFragmentVisitor**: Walks the parsed SQL AST via methods like `Visit(SelectStatement)`, `Visit(DeleteStatement)`, etc.
- **ErrorCallback**: Rules report violations via `errorCallback(ruleName, ruleText, lineNumber, columnNumber)`
- **Rule Registry**: RuleVisitorFriendlyNameTypeMap.Rules contains all available rules as a dictionary
- **Rule Severity**: Configured as "off", "warning", or "error" in .tsqllintrc
- **FixViolation**: Rules can implement automatic fixing logic in the `FixViolation` method
- **Rule Exceptions**: Inline comments like `/* tsqllint-disable rule-name */` suppress specific rules via RuleExceptionFinder

### Parsing Pipeline

1. **FragmentBuilder**: Uses Microsoft.SqlServer.TransactSql.ScriptDom.TSqlParser to parse SQL into AST
2. **RuleVisitorBuilder**: Creates visitor instances for enabled rules based on configuration
3. **SqlRuleVisitor**: Main walker that visits each fragment, notifying all rules
4. **SqlFileProcessor**: Orchestrates file discovery (via GlobPatternMatcher), parsing, and rule application
5. **ViolationFixer**: After linting, applies fixes from rules that report violations (iterative: max 10 passes)

### Command-Line Processing

- **CommandLineOptions**: Parses arguments
- **CommandLineOptionHandler**: Routes to appropriate **HandlerStrategy** (create config, print config, print plugins, validate paths, print usage/version)
- **Application.Run()**: Main orchestration - loads config, processes files, applies fixes if requested

### Configuration System

- **.tsqllintrc**: JSON config file (rule severities, compatibility level, plugins)
- **EnvironmentWrapper**: Resolves config file location (CLI arg → env var → local dir → home dir)
- **OverrideFinder**: Parses inline SQL comments for per-file rule/compatibility overrides
- **IgnoreListReader**: Loads .tsqllintignore patterns to exclude files from processing

## Testing Strategy

- Tests use **NUnit** framework
- **NSubstitute** for mocking interfaces
- **System.IO.Abstractions.TestingHelpers** for file system mocking (in-memory file system)
- Test SQL files are in `UnitTests/` and `FunctionalTests/` and copied to output directory at build
- Tests validate both rule detection and violation fixing

## Extending TSQLLint

### Adding a New Rule

1. Create a class in `source/TSQLLint.Infrastructure/Rules/` extending `BaseRuleVisitor`
2. Implement `RULE_NAME`, `RULE_TEXT`, `RULE_SEVERITY` properties
3. Override `Visit()` methods for relevant SQL statement types
4. Call `errorCallback()` to report violations
5. Optionally implement `FixViolation()` for automatic fixes
6. Add to RuleVisitorFriendlyNameTypeMap.Rules dictionary
7. Add unit tests and functional tests with sample SQL

### Creating a Plugin

Plugins implement **IPlugin** from TSQLLint.Common and can:
- Implement custom linting rules via `GetRules()` (returns `IDictionary<string, ISqlLintRule>`)
- Or perform custom actions via `PerformAction(IPluginContext context, IReporter reporter)`
- Must target net6.0+
- Referenced in .tsqllintrc under "plugins" with DLL path

## CI/CD Pipeline

CircleCI configuration (`.circleci/config.yml`):
- **build**: Runs tests on pull requests
- **build and notify**: On version tags, builds for all platforms and notifies Slack
- **package and push to nuget**: After approval, publishes to NuGet
- **release to github**: Creates GitHub release with artifacts
- Build matrix: linux-x64, linux-musl-x64, linux-musl-arm64, linux-arm, linux-arm64, osx-x64, osx-arm64, win-x64, win-x86, win-arm64
