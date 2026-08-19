# ktsu.RunCommand

A library that provides an easy way to execute a shell command and handle the output via delegates. It supports both synchronous and asynchronous execution with customizable output handling.

[![License](https://img.shields.io/github/license/ktsu-dev/RunCommand.svg?label=License&logo=nuget)](LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/ktsu.RunCommand?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.RunCommand?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.RunCommand?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/RunCommand?label=Commits&logo=github)](https://github.com/ktsu-dev/RunCommand/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/RunCommand?label=Contributors&logo=github)](https://github.com/ktsu-dev/RunCommand/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/RunCommand/dotnet.yml?branch=main&label=Build&logo=github)](https://github.com/ktsu-dev/RunCommand/actions)

## Installation

To install RunCommand, you can use the .NET CLI:

```bash
dotnet add package ktsu.RunCommand
```

Or you can use the NuGet Package Manager in Visual Studio to search for and install the ktsu.RunCommand package.

## Usage

### Basic Execution

The simplest way to execute a command is to use the `Execute` method, passing the executable and its arguments separately. All methods return the process exit code:

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

### Deprecated: single command strings

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

### Custom Output Handling

To handle the output of the command, you can provide delegates to the `OutputHandler` class:

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

> **_NOTE:_** _When using the default OutputHandler, the delegates will receive undelimited chunks of output. This gives you the flexibility to receive exactly the output the command produces, including whitespace and non-printable characters, and handle it as you see fit._

### Line-by-Line Output Handling

If you prefer to handle the output line by line, you can use the `LineOutputHandler` class:

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

All of the above examples can be executed asynchronously by using the `ExecuteAsync` method:

```csharp
using ktsu.RunCommand;

class Program
{
    static async Task Main()
    {
        int exitCode = await RunCommand.ExecuteAsync("dotnet", ["--version"]);

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

## Elevation (Windows)

If you need to run a command with elevated privileges, pass `Elevation.Elevated`. On Windows this launches the process with the `runas` verb, which triggers a UAC prompt:

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

Without a `WorkingDirectory` the process inherits the current directory of the calling process, which is what commands did before this option existed.

`EnvironmentVariables` is an overlay on the inherited environment, not a replacement: a name you do not list keeps whatever the calling process had. A `null` value removes a variable, which is how you unset something the parent had set:

```csharp
EnvironmentVariables = new Dictionary<string, string?>
{
    ["GIT_DIR"] = null,
}
```

Environment variables are the only control surface some tools expose, so this covers behaviour with no command-line equivalent — `GIT_TERMINAL_PROMPT=0` to make an authenticating `git fetch` fail rather than block forever on a prompt no terminal will answer, `GIT_ASKPASS`/`SSH_ASKPASS` to supply credentials without putting them on a command line where any process listing can read them, and `LC_ALL=C` to force stable, machine-parseable output rather than whatever the host locale produces.

> **_NOTE:_** _`EnvironmentVariables` cannot be combined with `Elevation.Elevated` on Windows. Elevation requires `UseShellExecute`, which offers nowhere to pass an environment, so the call throws `ArgumentException` rather than silently dropping the variables._

The type is `AbsoluteDirectoryPath` rather than a string on purpose. A relative directory would have to be resolved against the caller's current directory — the process-global state this option exists to avoid depending on, since it is shared by every thread and races with concurrent calls.

`CommandOptions.Elevation` carries the privilege level, so a single options object replaces the separate `Elevation` argument.

## Encoding

By default, the library uses the UTF-8 encoding for the input and output streams. If you need to use a different encoding, you can specify it in the `OutputHandler` or `LineOutputHandler` constructor:

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

## API Reference

### RunCommand Class

Passing the executable and its arguments separately:

-   `Execute(string fileName, IEnumerable<string> arguments)`: Executes a command synchronously and returns the process exit code.
-   `Execute(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler)`: Executes a command synchronously with custom output handling.
-   `ExecuteAsync(string fileName, IEnumerable<string> arguments)`: The asynchronous equivalent.
-   `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler)`: The asynchronous equivalent with custom output handling.
-   `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CancellationToken cancellationToken)`: As above, terminating the process and its children if the token is signalled.

**Obsolete** — see [Deprecated: single command strings](#deprecated-single-command-strings):

-   `Execute(string command)`, `Execute(string command, OutputHandler outputHandler)`, `Execute(string command, Elevation elevation)`, `Execute(string command, OutputHandler outputHandler, Elevation elevation)`
-   `ExecuteAsync(string command)`, `ExecuteAsync(string command, OutputHandler outputHandler)`, `ExecuteAsync(string command, Elevation elevation)`, `ExecuteAsync(string command, OutputHandler outputHandler, Elevation elevation)`, `ExecuteAsync(string command, CancellationToken cancellationToken)`, `ExecuteAsync(string command, OutputHandler outputHandler, CancellationToken cancellationToken)`, `ExecuteAsync(string command, OutputHandler outputHandler, Elevation elevation, CancellationToken cancellationToken)`
-   `Execute(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options)`: Executes a command synchronously with the given process options, passing arguments individually so no manual quoting is required.
-   `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options)`: The asynchronous equivalent.
-   `ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options, CancellationToken cancellationToken)`: As above, terminating the process and its children if the token is signalled.

### CommandOptions Record

-   `WorkingDirectory`: An `AbsoluteDirectoryPath` naming the directory the process starts in, or `null` to inherit the caller's current directory.
-   `EnvironmentVariables`: An `IReadOnlyDictionary<string, string?>` applied over the inherited environment, or `null` to inherit it unchanged. A `null` value removes a variable.
-   `Elevation`: The privilege level under which to run the command. Defaults to `Elevation.Default`.

### Elevation Enum

-   `Elevation.Default`: Run with the current process's privileges (output is captured).
-   `Elevation.Elevated`: On Windows, launch via the `runas` verb (UAC prompt); output is **not** captured. No-op on non-Windows.

-   ### OutputHandler Class

Processes output in raw chunks:

-   `OutputHandler(onStandardOutput, onStandardError)`: Constructor with handlers for output and error streams.

### LineOutputHandler Class

Processes output line by line:

-   `LineOutputHandler(onStandardOutput, onStandardError)`: Constructor with handlers for output and error streams.

> **_NOTE:_** _The `OutputHandler` classes receive undelimited chunks of output directly from the process stream. The `LineOutputHandler` buffers this output and splits it by newline characters, invoking the delegates for each complete line._

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## Acknowledgements

Thanks to the .NET community and ktsu.dev contributors for their support.
