// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand.Test;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[TestClass]
public class RunCommandTests
{
	private static string GetCopyCommand(string source, string destination) =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? $"cmd /c copy \"{source}\" \"{destination}\""
			: $"cp {source} {destination}";

	[TestMethod]
	public void ExecuteShouldExecuteCommandAndReturnExitCode()
	{
		string tempFile = Path.GetTempFileName();
		string destinationFile = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)}.{nameof(ExecuteShouldExecuteCommandAndReturnExitCode)}");

		File.Delete(destinationFile);

		string command = GetCopyCommand(tempFile, destinationFile);

		int exitCode = RunCommand.Execute(command);

		Assert.IsTrue(File.Exists(destinationFile), "Expected file to be created.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldExecuteCommandAndReturnExitCode()
	{
		string tempFile = Path.GetTempFileName();
		string destinationFile = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)}.{nameof(ExecuteAsyncShouldExecuteCommandAndReturnExitCode)}");

		File.Delete(destinationFile);

		string command = GetCopyCommand(tempFile, destinationFile);

		int exitCode = await RunCommand.ExecuteAsync(command).ConfigureAwait(false);

		Assert.IsTrue(File.Exists(destinationFile), "Expected file to be created.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public void ExecuteShouldReturnSuccessExitCodeForValidCommand()
	{
		// Using dotnet should be available in environments with .NET installed.
		string command = "dotnet --version";

		int exitCode = RunCommand.Execute(command);

		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldReturnSuccessExitCodeForValidCommand()
	{
		// Using dotnet should be available in environments with .NET installed.
		string command = "dotnet --version";

		int exitCode = await RunCommand.ExecuteAsync(command).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public void ExecuteShouldReturnNonZeroExitCodeForInvalidCommand()
	{
		// Using a command that should fail.
		string command = "dotnet --versionz";

		int exitCode = RunCommand.Execute(command);

		Assert.AreNotEqual(0, exitCode, "Expected exit code to be non-zero for failed command.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldReturnNonZeroExitCodeForInvalidCommand()
	{
		// Using a command that should fail.
		string command = "dotnet --versionz";

		int exitCode = await RunCommand.ExecuteAsync(command).ConfigureAwait(false);

		Assert.AreNotEqual(0, exitCode, "Expected exit code to be non-zero for failed command.");
	}

	[TestMethod]
	public void ExecuteShouldCaptureStandardOutputAndReturnExitCode()
	{
		List<string> outputCollector = [];

		// Using dotnet --version should be available in environments with .NET installed.
		string command = "dotnet --version";

		int exitCode = RunCommand.Execute(command, new OutputHandler(output =>
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		}));

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldCaptureStandardOutputAndReturnExitCode()
	{
		List<string> outputCollector = [];

		// Using dotnet --version should be available in environments with .NET installed.
		string command = "dotnet --version";

		int exitCode = await RunCommand.ExecuteAsync(command, new OutputHandler(output =>
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		})).ConfigureAwait(false);

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public void ExecuteShouldCaptureStandardOutputAndStandardErrorWithExitCode()
	{
		List<string> outputCollector = [];
		List<string> errorCollector = [];

		void onStandardOutput(string output)
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		}

		void onStandardError(string error)
		{
			if (!string.IsNullOrWhiteSpace(error))
			{
				errorCollector.Add(error);
			}
		}

		// Using dotnet --version should be available in environments with .NET installed.
		string command = "dotnet --version";
		int exitCode = RunCommand.Execute(command, new OutputHandler(onStandardOutput, onStandardError));

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.IsEmpty(errorCollector, "Expected standard error to be empty.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");

		outputCollector.Clear();
		errorCollector.Clear();

		// Using a command that should fail.
		command = "dotnet --versionz";
		exitCode = RunCommand.Execute(command, new OutputHandler(onStandardOutput, onStandardError));

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.IsNotEmpty(errorCollector, "Expected standard error to have content.");
		Assert.AreNotEqual(0, exitCode, "Expected exit code to be non-zero for failed command.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldCaptureStandardOutputAndStandardErrorWithExitCode()
	{
		List<string> outputCollector = [];
		List<string> errorCollector = [];

		void onStandardOutput(string output)
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		}

		void onStandardError(string error)
		{
			if (!string.IsNullOrWhiteSpace(error))
			{
				errorCollector.Add(error);
			}
		}

		// Using dotnet --version should be available in environments with .NET installed.
		string command = "dotnet --version";
		int exitCode = await RunCommand.ExecuteAsync(command, new OutputHandler(onStandardOutput, onStandardError)).ConfigureAwait(false);

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.IsEmpty(errorCollector, "Expected standard error to be empty.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");

		outputCollector.Clear();
		errorCollector.Clear();

		// Using a command that should fail.
		command = "dotnet --versionz";
		exitCode = await RunCommand.ExecuteAsync(command, new OutputHandler(onStandardOutput, onStandardError)).ConfigureAwait(false);

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.IsNotEmpty(errorCollector, "Expected standard error to have content.");
		Assert.AreNotEqual(0, exitCode, "Expected exit code to be non-zero for failed command.");
	}

	[TestMethod]
	[DataRow(Elevation.Default)]
	[DataRow(Elevation.Elevated)]
	public void ExecuteWithElevationShouldReturnExitCode(Elevation elevation)
	{
		// On non-Windows platforms Elevation.Elevated is a documented no-op, so both
		// values should run the command normally and return a zero exit code.
		// On Windows, Elevation.Elevated would trigger a UAC prompt, so only assert
		// the no-op case here.
		if (elevation == Elevation.Elevated && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Skipping elevated test on Windows to avoid UAC prompt.");
			return;
		}

		int exitCode = RunCommand.Execute("dotnet --version", elevation);

		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	[DataRow(Elevation.Default)]
	[DataRow(Elevation.Elevated)]
	public async Task ExecuteAsyncWithElevationShouldReturnExitCode(Elevation elevation)
	{
		if (elevation == Elevation.Elevated && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Skipping elevated test on Windows to avoid UAC prompt.");
			return;
		}

		int exitCode = await RunCommand.ExecuteAsync("dotnet --version", elevation).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public void ExecuteWithDefaultElevationAndHandlerShouldCaptureOutput()
	{
		List<string> outputCollector = [];

		int exitCode = RunCommand.Execute("dotnet --version", new OutputHandler(output =>
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		}), Elevation.Default);

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public async Task ExecuteAsyncWithDefaultElevationAndHandlerShouldCaptureOutput()
	{
		List<string> outputCollector = [];

		int exitCode = await RunCommand.ExecuteAsync("dotnet --version", new OutputHandler(output =>
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		}), Elevation.Default).ConfigureAwait(false);

		Assert.IsNotEmpty(outputCollector, "Expected standard output to have content.");
		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
	}

	[TestMethod]
	public void ExecuteShouldThrowArgumentNullExceptionWhenCommandIsNull()
	{
		bool didThrow = false;
		try
		{
			int exitCode = RunCommand.Execute(null!);
		}
		catch (AggregateException ex)
		{
			Assert.IsInstanceOfType<ArgumentNullException>(ex.InnerException);
			didThrow = true;
		}

		Assert.IsTrue(didThrow, "Expected an ArgumentNullException to be thrown.");
	}

	/// <summary>
	/// Returns a command that reads a single file, as an executable plus separate arguments. Both
	/// tools exit 0 only when they can open the file, so a path that was wrongly split on its
	/// spaces produces a non-zero exit code.
	/// </summary>
	private static (string FileName, string[] Arguments) GetReadFileCommand(string path) =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("certutil", ["-hashfile", path, "MD5"])
			: ("cat", [path]);

	/// <summary>
	/// Returns a command that runs for long enough to be cancelled mid-flight.
	/// </summary>
	private static (string FileName, string[] Arguments) GetSleepCommand() =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("ping", ["-n", "30", "127.0.0.1"])
			: ("sleep", ["30"]);

	// The caller name keeps each test on its own file, since tests run in parallel and would
	// otherwise collide writing a shared path.
	private static string WriteTempFileInDirectoryWithSpaces(string content, [CallerMemberName] string caller = "")
	{
		string directory = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)} with spaces");
		_ = Directory.CreateDirectory(directory);
		string path = Path.Join(directory, $"needle file {caller}.txt");

		// The trailing newline matters: LineOutputHandler only raises complete lines, so a match
		// that the search tool prints without a line terminator would sit unflushed in its buffer.
		File.WriteAllText(path, content + Environment.NewLine);
		return path;
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldPassArgumentContainingSpacesAsSingleArgument()
	{
		string path = WriteTempFileInDirectoryWithSpaces("hello");
		(string fileName, string[] arguments) = GetReadFileCommand(path);

		int exitCode = await RunCommand.ExecuteAsync(fileName, arguments).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, "Expected the path with spaces to arrive as one argument.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldFailWhenArgumentWithSpacesIsPassedAsOneString()
	{
		string path = WriteTempFileInDirectoryWithSpaces("hello");
		(string fileName, string[] arguments) = GetReadFileCommand(path);

		// The unquoted single-string overload splits the path on its spaces, which is precisely the
		// failure the argument-list overload exists to avoid. This pins that difference down.
		int exitCode = await RunCommand.ExecuteAsync($"{fileName} {string.Join(" ", arguments)}").ConfigureAwait(false);

		Assert.AreNotEqual(0, exitCode, "Expected the unquoted command string to mis-split the path.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldCaptureOutputWhenGivenArgumentList()
	{
		List<string> output = [];
		LineOutputHandler handler = new(onStandardOutput: output.Add);

		int exitCode = await RunCommand.ExecuteAsync("dotnet", ["--version"], handler).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
		Assert.IsNotEmpty(output.Where(line => !string.IsNullOrWhiteSpace(line)));
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldThrowWhenTokenIsAlreadyCancelled()
	{
		using CancellationTokenSource cancellationTokenSource = new();
		await cancellationTokenSource.CancelAsync().ConfigureAwait(false);

		await Assert.ThrowsAsync<OperationCanceledException>(
			() => RunCommand.ExecuteAsync("dotnet --version", cancellationTokenSource.Token)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldTerminateProcessWhenCancelledWhileRunning()
	{
		using CancellationTokenSource cancellationTokenSource = new();
		(string fileName, string[] arguments) = GetSleepCommand();

		Task<int> execution = RunCommand.ExecuteAsync(fileName, arguments, new OutputHandler(), cancellationTokenSource.Token);
		await cancellationTokenSource.CancelAsync().ConfigureAwait(false);

		// The command sleeps for 30 seconds, so returning at all proves the process was killed
		// rather than merely abandoned.
		await Assert.ThrowsAsync<OperationCanceledException>(() => execution).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldThrowRatherThanReturnAnExitCodeWhenCancellationWinsTheRace()
	{
		(string fileName, string[] arguments) = GetSleepCommand();

		// Cancelling this close to the start puts two paths in a near dead heat: the registration
		// kills the process, and the kill makes it exit fast enough that the wait can observe a
		// normal exit before it observes the token. Losing that race returns the killed process's
		// exit code instead of throwing, so a caller cannot tell cancellation from real failure.
		// A single attempt still throws most of the time, which is why this repeats: 50 attempts
		// make a false pass vanishingly unlikely.
		for (int attempt = 0; attempt < 50; attempt++)
		{
			using CancellationTokenSource cancellationTokenSource = new();
			cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(1));

			await Assert.ThrowsAsync<OperationCanceledException>(
				() => RunCommand.ExecuteAsync(fileName, arguments, new OutputHandler(), cancellationTokenSource.Token),
				$"Attempt {attempt} returned an exit code instead of throwing.").ConfigureAwait(false);
		}
	}
}
