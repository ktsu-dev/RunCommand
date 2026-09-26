# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Restore, build, and test (standard workflow)
dotnet restore
dotnet build

# Build specific configuration
dotnet build -c Release

# Create the NuGet package
dotnet pack
```

### Running Tests

The test project uses MSTest.Sdk with the Microsoft Testing Platform (MTP). `dotnet test` reports
`Zero tests ran` here even though the tests build and discover correctly — run the produced test
executable directly instead:

```bash
# Run all tests
./RunCommand.Test/bin/Debug/net10.0/ktsu.RunCommand.Test.exe

# Run a single test
./RunCommand.Test/bin/Debug/net10.0/ktsu.RunCommand.Test.exe --filter "FullyQualifiedName~ExecuteAsyncShouldStartTheProcessInTheGivenWorkingDirectory"

# List the discovered tests
./RunCommand.Test/bin/Debug/net10.0/ktsu.RunCommand.Test.exe --list-tests
```

The executable takes VSTest-style `--filter` expressions. The MTP-native `--filter-method` and
`--treenode-filter` options are not accepted by this test host.

Two elevation tests self-skip on Windows (`Assert.Inconclusive`) to avoid raising a UAC prompt, so
a clean run reports skips rather than failures.

## Project Structure

This is a .NET library (`ktsu.RunCommand`) that executes external commands and delivers their
output through delegates. The solution uses:

- **ktsu.Sdk** - Custom SDK providing shared build configuration
- **MSTest.Sdk** - Test project SDK with Microsoft Testing Platform
- Multi-targeting: `net10.0`, `net9.0`, `net8.0`, `net7.0`, `net6.0`, `net5.0`, `netstandard2.0`, `netstandard2.1`

The test project targets `net10.0` only.

### Key Files

- `RunCommand/RunCommand.cs` - Static class holding the whole public execution API and the private `CreateStartInfo`/`RunAsync`/`TryKill` core
- `RunCommand/CommandOptions.cs` - Record carrying process-shaping settings (working directory, environment variables, elevation, standard input)
- `RunCommand/OutputHandler.cs` - Base output handler delivering raw chunks
- `RunCommand/LineOutputHandler.cs` - Derived handler that buffers chunks into complete lines
- `RunCommand/AsyncProcessStreamReader.cs` - Internal concurrent reader for stdout and stderr
- `RunCommand/Elevation.cs` - Enum selecting the privilege level
- `RunCommand/StandardInputMode.cs` - Enum selecting what standard input is connected to

### Dependencies

- **ktsu.Semantics.Paths** - Supplies `AbsoluteDirectoryPath` for `CommandOptions.WorkingDirectory`
- **ktsu.Semantics.Strings** - Referenced explicitly because `WeakString` is declared there; the SDK's `KTSU0006` analyzer rejects using it transitively through Paths
- **Polyfill** (`PrivateAssets="all"`) - Supplies `Ensure.NotNull` and newer-framework APIs on older targets
- **System.Memory**, **System.Threading.Tasks.Extensions** - `netstandard2.0`/`netstandard2.1` only

Note that the Semantics packages pull `System.Text.Json`, `System.IO.Pipelines` and
`System.Text.Encodings.Web` in transitively. On `net5.0`–`net7.0` those emit "doesn't support
<tfm>" MSBuild warnings. They come from targets files rather than the compiler, so
`TreatWarningsAsErrors` does not escalate them and the build stays green.

## Architecture

### Public API shape

Two entry shapes exist, both returning the process exit code:

- **Argument vector** — `Execute`/`ExecuteAsync(string fileName, IEnumerable<string> arguments, ...)`. Preferred. The executable is passed separately, so nothing has to be quoted.
- **Command string** — `Execute`/`ExecuteAsync(string command, ...)`. **Obsolete.** Splits on the first space, so an executable path containing a space is mis-split. Kept working for compatibility; do not add new overloads to this shape.

Optional settings arrive through `CommandOptions` rather than through new parameters, so adding a
setting costs no new overloads. The older overloads taking a bare `Elevation` delegate through
`new CommandOptions { Elevation = elevation }`.

### Key design patterns

1. **Async over sync**: The synchronous `Execute` methods call `ExecuteAsync().Result`, making the async implementation the single source of truth. Note this means argument-null exceptions surface wrapped in `AggregateException` from the synchronous overloads.

2. **Strategy pattern**: `OutputHandler` and `LineOutputHandler` plug different output processing strategies into the same execution core.

3. **Template method**: `OutputHandler` exposes virtual `HandleStandardOutputData`/`HandleStandardErrorData` that `LineOutputHandler` overrides.

4. **Buffering strategy**: `LineOutputHandler` keeps separate `outputBuffer`/`errorBuffer` fields so an incomplete line spanning two chunk reads is reassembled rather than raised twice.

## Important Implementation Notes

### Cancellation

`RunAsync` delivers cancellation two ways at once: a `CancellationTokenRegistration` kills the
process, and `WaitForExitAsync(cancellationToken)` separately observes the token. Killing the
process makes it exit fast enough that the normal-exit path can win that race, which would return
the killed process's exit code (`-1` on Windows) and throw nothing.

`RunAsync` therefore calls `cancellationToken.ThrowIfCancellationRequested()` after the await and
before returning. **Do not remove this** — without it a cancelled command is indistinguishable from
a genuine failure of the underlying tool. The regression test repeats a 1 ms cancellation 50 times,
because a single attempt still throws most of the time even when the bug is present.

Process-tree termination requires .NET Core 3.0 or later; the `netstandard2.0`/`netstandard2.1`
builds can only kill the process itself.

### Elevation constraints

Elevation forces `UseShellExecute = true`, which is incompatible with output redirection, setting an
environment, and redirecting standard input. Consequently an `OutputHandler` is silently not invoked
under elevation (documented behaviour), while combining either `EnvironmentVariables` or
`StandardInputMode.Closed` with elevation throws `ArgumentException` up front rather than failing
opaquely inside `Process.Start`.

### Standard input

Standard input is inherited by default, which is what commands did before `CommandOptions.StandardInput`
existed and what an interactive command needs. `StandardInputMode.Closed` redirects it and closes the
stream immediately after `Process.Start`, so a command that reads it sees end of stream.

Both halves are load-bearing. Redirecting alone leaves the command holding a pipe nobody writes to,
which is the same wait as inheriting; closing is what turns a read into end of stream. Callers that
are not consoles — services, daemons, background workers — want `Closed`, because an inherited handle
that stays open without producing data turns a command that reads it into a hang that ends only on
cancellation.

Testing this needs care. A test runner whose own standard input is already at end of stream hands a
child the same answer by inheritance, so a behavioural test alone passes even when the option is
ignored entirely. `ExecuteAsyncShouldGiveTheCommandItsOwnStandardInputWhenClosed` compares
`/proc/self/fd/0` between the caller and the command to pin the redirection itself, and is Linux-only
for that reason.

### Argument escaping

`ProcessStartInfo.ArgumentList` is unavailable on `netstandard2.0`, so that target alone falls back
to a hand-written `EscapeArgument` implementing the `CommandLineToArgvW` quoting rules. This is one
of the few places conditional compilation is warranted.

### Stream reading

`AsyncProcessStreamReader` reads stdout and stderr concurrently with 4096-character buffers and
performs a final read after process exit, which is what ensures short-lived processes do not lose
buffered output.

### Process configuration

On Windows, `LoadUserProfile` is set to true for proper environment variable expansion.

## Testing

Tests live in `RunCommand.Test/` and use MSTest:

- `RunCommandTests.cs` - Execution, output capture, elevation, working directory, environment variables, and cancellation
- `LineOutputHandlerTests.cs` - Line buffering across chunk boundaries

Patterns worth preserving:

- Commands are chosen per-platform through helpers (`GetSleepCommand`, `GetPrintWorkingDirectoryCommand`, `GetPrintEnvironmentVariableCommand`) rather than hard-coded, so the suite runs on Windows and Unix.
- Tests run in parallel at method level, so anything touching process-global or filesystem state derives a unique name from `[CallerMemberName]`.
- The tests covering the obsolete command-string overloads sit inside a single `#pragma warning disable CS0618` region that ends after the last of them. Keep the region tight; do not promote it to file scope.

## Version Management

Semantic versioning with git-based version calculation:

- Version tags in commit messages: `[major]`, `[minor]`, `[patch]`, `[pre]`
- Public API changes are automatically detected and trigger minor version bumps
- VERSION.md, CHANGELOG.md, and LICENSE.md are auto-generated — never edit them manually

## CI/CD

Uses `scripts/PSBuild.psm1` PowerShell module for CI pipeline. Version increments are controlled by commit message tags: `[major]`, `[minor]`, `[patch]`, `[pre]`.

## Code Quality

Do not add global suppressions for warnings. Use explicit suppression attributes with justifications when needed, with preprocessor defines only as fallback. Make the smallest, most targeted suppressions possible.
