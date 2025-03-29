# ktsu.RunCommand

A library that provides an easy way to execute a shell command and output via delegates.

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

Here's a simple example of how to use the library. In this example, we execute a shell command and print both standard output and error:

```csharp
using ktsu.RunCommand;

class Program
{
    static void Main(string[] args)
    {
        string command = "echo Hello World!";

        RunCommand.Execute(
            command,
            onStandardOutput: Console.Write,
            onStandardError: Console.Write
        );
    }
}

```

NOTE: The delegates will receive undelimited chunks of output. This gives you the flexibility to receive exactly the output the command produces, and handle it as you see fit.

You may wish to buffer the output and split it by newline characters manually, but many console commands will output non-printable characters that are intended to be interpreted by the terminal.

If you need to split the output by newline characters, you can use the following as an example:

```csharp
using ktsu.RunCommand;

class Program
{
    private static string outputBuffer = "";
    private static string errorBuffer = "";

    static void Main(string[] args)
    {
        string command = "echo Hello World!";

        RunCommand.Execute(
            command,
            output => ProcessByLine(output, ref outputBuffer, s => Console.WriteLine($"Output: {s}")),
            error => ProcessByLine(error, ref errorBuffer, s => Console.WriteLine($"Error: {s}"))
        );
    }

    private static void ProcessByLine(string output, ref string buffer, Action<string> onLineReceived)
    {
        buffer += output;
        while (buffer.Contains('\n'))
        {
            string[] split = buffer.Split('\n', 2);
            buffer = split.Length == 1
                ? string.Empty
                : split[1];

            onLineReceived(split[0]);
        }
    }
}
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## Acknowledgements

Thanks to the .NET community and ktsu.dev contributors for their support.
