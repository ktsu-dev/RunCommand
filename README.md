# ktsu.RunCommand

> A .NET library for executing external commands and handling their output through delegates, with synchronous and asynchronous APIs, cancellation, and control over the spawned process.

[![License](https://img.shields.io/github/license/ktsu-dev/RunCommand.svg?label=License&logo=nuget)](LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/ktsu.RunCommand?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.RunCommand?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.RunCommand?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/RunCommand?label=Commits&logo=github)](https://github.com/ktsu-dev/RunCommand/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/RunCommand?label=Contributors&logo=github)](https://github.com/ktsu-dev/RunCommand/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/RunCommand/dotnet.yml?branch=main&label=Build&logo=github)](https://github.com/ktsu-dev/RunCommand/actions)

## Introduction

`ktsu.RunCommand` runs an external command and hands you its output as it arrives, instead of making you assemble `Process`, `ProcessStartInfo`, redirected streams and exit-code plumbing yourself. Output is delivered through delegates — either as raw chunks exactly as the process emits them, or buffered into complete lines — and every method returns the process exit code.

Arguments are passed as a vector rather than as one string, so a path containing spaces needs no manual quoting and cannot be mis-split. The process itself can be shaped through a working directory and an environment variable overlay, run elevated on Windows, and terminated along with its children through a cancellation token.

## Features

-   **Delegate-based output**: Receive standard output and standard error through `Action<string>` delegates as the process produces them, rather than waiting for it to exit.
-   **Raw or line-buffered**: `OutputHandler` delivers undelimited chunks exactly as they arrive; `LineOutputHandler` buffers across chunks and raises one call per complete line.
-   **Synchronous and asynchronous**: Every operation is available as both `Execute` and `ExecuteAsync`, with the asynchronous implementation as the single source of truth.
-   **Quote-free arguments**: Pass the executable and each argument separately, so spaces in paths and arguments are handled by the platform rather than by string concatenation.
-   **Working directory**: Start the process in a specific directory without mutating the process-global current directory.
-   **Environment variables**: Apply an overlay over the inherited environment for a single call, adding, overriding, or removing individual variables.
-   **Cancellation**: A signalled `CancellationToken` terminates the process and always surfaces as an `OperationCanceledException`, never as a synthetic exit code.
-   **Windows elevation**: Launch through the `runas` verb for a UAC-elevated process.
-   **Custom encoding**: Decode the output streams with any `Encoding`; defaults to UTF-8.
-   **Broad target support**: .NET Standard 2.0 and 2.1 through .NET 10.

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.RunCommand
```

### .NET CLI

```bash
dotnet add package ktsu.RunCommand
```

### Package Reference

```xml
<PackageReference Include="ktsu.RunCommand" Version="1.5.0" />
```

## Usage Examples

### Basic Example

Pass the executable and its arguments separately. All methods return the process exit code:

```csharp
using ktsu.RunCommand;

class Program
{
    static void Main()
    {
        int exitCode = RunCommand.Execute("dotnet", ["--version"]);

        if (exitCode == 0)
        {
            Console.WriteLine("Command executed successfully!");
        }
        else
        {
            Console.WriteLine($"Command failed with exit code: {exitCode}");
        }
    }
}
```

### Custom Output Handling

To handle the output of the command, provide delegates to the `OutputHandler` class:

```csharp
using ktsu.RunCommand;

class Program
{
    static void Main()
    {
        int exitCode = RunCommand.Execute(
            fileName: "dotnet",
            arguments: ["--version"],
            outputHandler: new(
                onStandardOutput: Console.Write,
                onStandardError: Console.Write
            )
        );

        Console.WriteLine($"Process exited with code: {exitCode}");
    }
}
```

> **_NOTE:_** _When using the default `OutputHandler`, the delegates receive undelimited chunks of output. This gives you exactly what the command produces, including whitespace and non-printable characters, to handle as you see fit._

### Line-by-Line Output Handling

To handle the output one line at a time, use the `LineOutputHandler` class:

```csharp
using ktsu.RunCommand;

class Program
{
    static void Main()
    {
        int exitCode = RunCommand.Execute(
            fileName: "dotnet",
            arguments: ["--version"],
            outputHandler: new LineOutputHandler(
                onStandardOutput: line => Console.WriteLine($"Output: {line}"),
                onStandardError: line => Console.WriteLine($"Error: {line}")
            )
        );

        Console.WriteLine($"Process exited with code: {exitCode}");
    }
}
```

### Asynchronous Execution

All of the above examples can be run asynchronously with `ExecuteAsync`:

```csharp
using ktsu.RunCommand;

class Program
{
    static async Task Main()
    {
        int exitCode = await RunCommand.ExecuteAsync("dotnet", ["--version"]);

        Console.WriteLine($"Process exited with code: {exitCode}");
    }
}
```

### Cancellation

Passing a `CancellationToken` terminates the process when the token is signalled:

```csharp
using ktsu.RunCommand;

class Program
{
    static async Task Main()
    {
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));

        try
        {
            int exitCode = await RunCommand.ExecuteAsync(
                fileName: "dotnet",
                arguments: ["build"],
                outputHandler: new LineOutputHandler(onStandardOutput: Console.WriteLine),
                cancellationToken: cancellation.Token);

            Console.WriteLine($"Process exited with code: {exitCode}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("The command was cancelled.");
        }
    }
}
```

A cancelled call always throws `OperationCanceledException` — it never returns the killed process's exit code — so cancellation cannot be mistaken for a genuine failure of the command.

On .NET Core 3.0 and later the entire process tree is terminated. On .NET Standard 2.0 and 2.1 only the process itself can be terminated, so any grandchildren it spawned are left running.

## Process Options

`CommandOptions` shapes the process a command runs in. Pass it alongside an executable and its arguments:

```csharp
using ktsu.RunCommand;
using ktsu.Semantics.Paths;

class Program
{
    static async Task Main()
    {
        int exitCode = await RunCommand.ExecuteAsync(
            fileName: "git",
            arguments: ["status", "--short"],
            outputHandler: new LineOutputHandler(onStandardOutput: Console.WriteLine),
            options: new()
            {
                WorkingDirectory = AbsoluteDirectoryPath.Create(@"C:\repos\my project"),
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["GIT_TERMINAL_PROMPT"] = "0",
                    ["LC_ALL"] = "C",
                },
            });

        Console.WriteLine($"Process exited with code: {exitCode}");
    }
}
```

`CommandOptions.Elevation` carries the privilege level too, so a single options object replaces the separate `Elevation` argument.

### Working Directory

Without a `WorkingDirectory` the process inherits the current directory of the calling process, which is what commands did before this option existed.

The type is `AbsoluteDirectoryPath` rather than a string on purpose. A relative directory would have to be resolved against the caller's current directory — the process-global state this option exists to avoid depending on, since it is shared by every thread and races with concurrent calls.

### Environment Variables

`EnvironmentVariables` is an overlay on the inherited environment, not a replacement: a name you do not list keeps whatever the calling process had. A `null` value removes a variable, which is how you unset something the parent had set:

```csharp
EnvironmentVariables = new Dictionary<string, string?>
{
    ["GIT_DIR"] = null,
}
```

Environment variables are the only control surface some tools expose, so this covers behaviour with no command-line equivalent — `GIT_TERMINAL_PROMPT=0` to make an authenticating `git fetch` fail rather than block forever on a prompt no terminal will answer, `GIT_ASKPASS`/`SSH_ASKPASS` to supply credentials without putting them on a command line where any process listing can read them, and `LC_ALL=C` to force stable, machine-parseable output rather than whatever the host locale produces.

> **_NOTE:_** _`EnvironmentVariables` cannot be combined with `Elevation.Elevated` on Windows. Elevation requires `UseShellExecute`, which offers nowhere to pass an environment, so the call throws `ArgumentException` rather than silently dropping the variables._

## Elevation (Windows)

To run a command with elevated privileges, set `Elevation.Elevated`. On Windows this launches the process with the `runas` verb, which triggers a UAC prompt:

```csharp
using ktsu.RunCommand;

class Program
{
    static void Main()
    {
        int exitCode = RunCommand.Execute(
            fileName: "powershell",
            arguments: ["-Command", "Get-Service"],
            outputHandler: new(),
            options: new() { Elevation = Elevation.Elevated });

        Console.WriteLine($"Process exited with code: {exitCode}");
    }
}
```

> **_NOTE:_** _Output redirection is incompatible with `runas`, so an `OutputHandler` passed alongside `Elevation.Elevated` will **not** be invoked. You still get the process exit code._

On non-Windows platforms `Elevation.Elevated` is a no-op — prefix your command with `sudo` yourself if you need elevation there.

## Encoding

By default the library decodes the output streams as UTF-8. To use a different encoding, specify it in the `OutputHandler` or `LineOutputHandler` constructor:

```csharp
using System.Text;
using ktsu.RunCommand;

class Program
{
    static void Main()
    {
        int exitCode = RunCommand.Execute(
            fileName: "dotnet",
            arguments: ["--version"],
            outputHandler: new(
                onStandardOutput: Console.Write,
                onStandardError: Console.Write,
                encoding: Encoding.ASCII
            )
        );
    }
}
```

## Deprecated: Single Command Strings

The overloads taking one `command` string are obsolete. They separate the executable from its arguments by splitting on the **first space**, which cannot represent an executable path that itself contains a space — on Windows that includes anything under `C:\Program Files\`:

```csharp
// Obsolete, and broken: splits into "C:\Program" plus "Files\Git\bin\git.exe --version"
await RunCommand.ExecuteAsync(@"C:\Program Files\Git\bin\git.exe --version");

// Correct
await RunCommand.ExecuteAsync(@"C:\Program Files\Git\bin\git.exe", ["--version"]);
```

Quoting does not rescue it, because the split happens before any quote handling. The string form is inherently ambiguous — no parse handles every combination of spaces and quotes without adopting a shell's full grammar — so rather than grow a half-grammar that moves the surprise elsewhere, these overloads are deprecated in favour of the argument-list ones, which have no such ambiguity because the executable is passed separately.

Migration is mechanical: split the string yourself at the boundaries you meant.

| Obsolete | Replacement |
| --- | --- |
| `Execute(command)` | `Execute(fileName, arguments)` |
| `Execute(command, outputHandler)` | `Execute(fileName, arguments, outputHandler)` |
| `Execute(command, elevation)` | `Execute(fileName, arguments, outputHandler, options)` |
| `ExecuteAsync(command)` | `ExecuteAsync(fileName, arguments)` |
| `ExecuteAsync(command, outputHandler)` | `ExecuteAsync(fileName, arguments, outputHandler)` |
| `ExecuteAsync(command, cancellationToken)` | `ExecuteAsync(fileName, arguments, outputHandler, cancellationToken)` |
| `ExecuteAsync(command, outputHandler, elevation, cancellationToken)` | `ExecuteAsync(fileName, arguments, outputHandler, options, cancellationToken)` |

## API Reference

### `RunCommand`

Static class providing the command execution API. Every method returns the process exit code.

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `Execute(string fileName, IEnumerable<string> arguments)` | `int` | Executes a command synchronously. |
| `Execute(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler)` | `int` | Executes a command synchronously with custom output handling. |
| `Execute(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options)` | `int` | Executes a command synchronously with the given process options. |
| `ExecuteAsync(string fileName, IEnumerable<string> arguments)` | `Task<int>` | Executes a command asynchronously. |
| `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler)` | `Task<int>` | Executes a command asynchronously with custom output handling. |
| `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CancellationToken cancellationToken)` | `Task<int>` | As above, terminating the process and its children if the token is signalled. |
| `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, Elevation elevation, CancellationToken cancellationToken)` | `Task<int>` | As above, at the given elevation level. |
| `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options)` | `Task<int>` | Executes a command asynchronously with the given process options. |
| `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options, CancellationToken cancellationToken)` | `Task<int>` | As above, terminating the process and its children if the token is signalled. |

The overloads taking a single `command` string — four `Execute` and seven `ExecuteAsync` — are **obsolete**. See [Deprecated: Single Command Strings](#deprecated-single-command-strings) for the migration table.

### `CommandOptions`

Record describing how to shape the process a command runs in. Every member defaults to the behaviour commands had before the type existed, so an instance with nothing set is equivalent to not passing one at all.

#### Properties

| Name | Type | Description |
|------|------|-------------|
| `WorkingDirectory` | `AbsoluteDirectoryPath?` | The directory the process starts in, or `null` to inherit the caller's current directory. |
| `EnvironmentVariables` | `IReadOnlyDictionary<string, string?>?` | Variables applied over the inherited environment, or `null` to inherit it unchanged. A `null` value removes a variable. |
| `Elevation` | `Elevation` | The privilege level under which to run the command. Defaults to `Elevation.Default`. |

### `OutputHandler`

Processes output in raw, undelimited chunks as they arrive from the process.

#### Constructor

| Name | Description |
|------|-------------|
| `OutputHandler(Action<string>? onStandardOutput = null, Action<string>? onStandardError = null, Encoding? encoding = null)` | Creates a handler with delegates for the output and error streams. `encoding` defaults to UTF-8. |

#### Properties

| Name | Type | Description |
|------|------|-------------|
| `Encoding` | `Encoding` | The encoding used to decode the process's output streams. |

### `LineOutputHandler`

Inherits from `OutputHandler` and buffers incoming chunks, invoking the delegates once per complete line. Incomplete trailing data is held until the rest of the line arrives.

#### Constructor

| Name | Description |
|------|-------------|
| `LineOutputHandler(Action<string>? onStandardOutput = null, Action<string>? onStandardError = null, Encoding? encoding = null)` | Creates a line-buffering handler with delegates for the output and error streams. |

### `Elevation`

Enum specifying the privilege level under which a command runs.

| Name | Description |
|------|-------------|
| `Default` | Run with the current process's privileges. Standard output and standard error are captured. |
| `Elevated` | On Windows, launch through the `runas` verb, prompting for UAC consent; output is **not** captured. No effect on non-Windows platforms. |

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## License

This project is licensed under the MIT License. See the [LICENSE.md](LICENSE.md) file for details.
