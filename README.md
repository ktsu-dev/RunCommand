# ktsu.RunCommand

A library that provides an easy way to execute a shell command and handle the output via delegates. It supports both synchronous and asynchronous execution with customizable output handling.

[![License](https://img.shields.io/github/license/ktsu-dev/RunCommand.svg?label=License&logo=nuget)](LICENSE.md)

[![NuGet Version](https://img.shields.io/nuget/v/ktsu.RunCommand?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.RunCommand?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.RunCommand?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.RunCommand)

[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/RunCommand?label=Commits&logo=github)](https://github.com/ktsu-dev/RunCommand/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/RunCommand?label=Contributors&logo=github)](https://github.com/ktsu-dev/RunCommand/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/RunCommand/dotnet.yml?label=Build&logo=github)](https://github.com/ktsu-dev/RunCommand/actions)

## Installation

To install RunCommand, you can use the .NET CLI:

```bash
dotnet add package ktsu.RunCommand
```

Or you can use the NuGet Package Manager in Visual Studio to search for and install the ktsu.RunCommand package.

## Usage

### Basic Execution

The simplest way to execute a command is to use the `Execute` method. All methods return the process exit code:

```csharp
using ktsu.RunCommand;

class Program
{
    static void Main()
    {
        int exitCode = RunCommand.Execute("echo Hello World!");

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

To handle the output of the command, you can provide delegates to the `OutputHandler` class:

```csharp
using ktsu.RunCommand;

class Program
{
    static void Main()
    {
        int exitCode = RunCommand.Execute(
            command: "echo Hello World!",
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
            command: "echo Hello World!",
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
        int exitCode = await RunCommand.ExecuteAsync("echo Hello World!");

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
            command: "echo Hello World!",
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

-   `Execute(string command)`: Executes a command synchronously and returns the process exit code.
-   `Execute(string command, OutputHandler outputHandler)`: Executes a command synchronously with custom output handling and returns the process exit code.
-   `ExecuteAsync(string command)`: Executes a command asynchronously and returns a task with the process exit code.
-   `ExecuteAsync(string command, OutputHandler outputHandler)`: Executes a command asynchronously with custom output handling and returns a task with the process exit code.

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
