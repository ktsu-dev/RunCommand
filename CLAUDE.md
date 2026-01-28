# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
dotnet build                                    # Build the solution
dotnet test                                     # Run all tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run specific test
```

## Architecture

This is a .NET library (`ktsu.RunCommand`) that provides shell command execution with delegate-based output handling. It uses the `ktsu.Sdk` for project configuration.

### Core Components

- **RunCommand** (`RunCommand/RunCommand.cs`) - Static class with `Execute` and `ExecuteAsync` methods that spawn processes, wire up output handling, and return exit codes
- **OutputHandler** (`RunCommand/OutputHandler.cs`) - Base class for processing stdout/stderr in raw chunks via delegates
- **LineOutputHandler** (`RunCommand/LineOutputHandler.cs`) - Subclass that buffers output and delivers it line-by-line
- **AsyncProcessStreamReader** (`RunCommand/AsyncProcessStreamReader.cs`) - Internal class that continuously reads process streams asynchronously using 4KB buffers

### Data Flow

1. `RunCommand.ExecuteAsync` splits the command string and creates a `Process` with redirected streams
2. `AsyncProcessStreamReader` reads from stdout/stderr in parallel using `ReadAsync`
3. Output chunks are passed to `OutputHandler.HandleStandardOutputData`/`HandleStandardErrorData`
4. `LineOutputHandler` (if used) buffers chunks and splits on newlines before invoking delegates

### Multi-Targeting

The library targets netstandard2.0, netstandard2.1, and net5.0-net10.0. The test project targets net9.0 only.

## Code Style

- Tabs for indentation in C# files
- File-scoped namespaces with usings inside namespace
- Prefer `var` is disabled (`csharp_style_var_*: false`)
- All analyzer diagnostics treated as errors
- Use `Ensure.NotNull()` for parameter validation (provided by ktsu.Sdk)
