// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand.Test;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices;
using ktsu.Semantics.Paths;

[TestClass]
public class RunCommandTests
{
	private static string GetCopyCommand(string source, string destination) =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? $"cmd /c copy \"{source}\" \"{destination}\""
			: $"cp {source} {destination}";

	// These tests cover the command-string overloads themselves, which are obsolete but still
	// supported, so they have to keep calling them until those overloads are removed. The region
	// ends after the last such test rather than covering the file.
#pragma warning disable CS0618 // Type or member is obsolete

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

#pragma warning restore CS0618 // Type or member is obsolete

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
	/// Returns a command that prints the directory its process was started in.
	/// </summary>
	private static (string FileName, string[] Arguments) GetPrintWorkingDirectoryCommand() =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("cmd", ["/c", "cd"])
			: ("pwd", []);

	// The caller name keeps each test on its own directory, since tests run in parallel.
	private static string CreateDirectoryForTest([CallerMemberName] string caller = "") =>
		Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)} {caller}")).FullName;

	/// <summary>
	/// Returns a command that prints the value of an environment variable, wrapped in brackets so
	/// that an empty value is still distinguishable from no output at all.
	/// </summary>
	private static (string FileName, string[] Arguments) GetPrintEnvironmentVariableCommand(string name) =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("cmd", ["/c", $"echo [%{name}%]"])
			: ("sh", ["-c", $"echo \"[${name}]\""]);

	// Tests run in parallel and the host environment is process-wide, so each test needs its own
	// variable name to avoid stepping on another test's value.
	private static string EnvironmentVariableNameFor([CallerMemberName] string caller = "") =>
		$"RUNCOMMAND_TEST_{caller.ToUpperInvariant()}";

	private static async Task<string> ReadEnvironmentVariableFromChildAsync(string name, CommandOptions options)
	{
		(string fileName, string[] arguments) = GetPrintEnvironmentVariableCommand(name);
		List<string> output = [];

		int exitCode = await RunCommand.ExecuteAsync(
			fileName,
			arguments,
			new LineOutputHandler(onStandardOutput: output.Add),
			options).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, "Expected the command to run successfully.");
		return string.Concat(output).Trim();
	}

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
#pragma warning disable CS0618 // Type or member is obsolete -- this test exists to pin the very
		// behaviour that made the command-string overloads obsolete, so it must call one.
		int exitCode = await RunCommand.ExecuteAsync($"{fileName} {string.Join(" ", arguments)}").ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

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
			() => RunCommand.ExecuteAsync("dotnet", ["--version"], new OutputHandler(), cancellationTokenSource.Token)).ConfigureAwait(false);
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

	[TestMethod]
	public async Task ExecuteAsyncShouldReturnWhenCancelledWhileADetachedDescendantHoldsTheOutputPipe()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs a shell that can orphan a child out of its own process tree while that child keeps the pipe it inherited. The wait this covers is in platform independent code, so the other legs cover it.");
		}

		using CancellationTokenSource cancellationTokenSource = new();

		// The inner shell backgrounds a sleep and exits immediately, so that sleep is reparented to
		// init and is no longer a descendant the entire-process-tree kill can walk to — but it still
		// holds the standard output and standard error handles it inherited. The outer sleep keeps
		// the process this call owns alive, so cancellation is what ends it. Killing that process
		// therefore closes neither pipe's write end, and end of stream never arrives.
		//
		// setsid is not enough here: it gives the child its own session but leaves its parent alone,
		// so the kill still reaches it.
		Task<int> execution = RunCommand.ExecuteAsync(
			"sh",
			["-c", "sh -c 'sleep 30 &'; sleep 30"],
			new OutputHandler(),
			cancellationTokenSource.Token);

		await cancellationTokenSource.CancelAsync().ConfigureAwait(false);

		// Bounded rather than a bare await: before the fix this call never returns, and a test that
		// hangs takes the whole run down with it instead of reporting a failure.
		Task finished = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);

		Assert.AreSame(
			execution,
			finished,
			"Expected a cancelled call to return promptly rather than wait on a pipe an orphaned descendant still holds open.");

		await Assert.ThrowsAsync<OperationCanceledException>(() => execution).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldStartTheProcessInTheGivenWorkingDirectory()
	{
		string directory = CreateDirectoryForTest();
		(string fileName, string[] arguments) = GetPrintWorkingDirectoryCommand();
		List<string> output = [];

		int exitCode = await RunCommand.ExecuteAsync(
			fileName,
			arguments,
			new LineOutputHandler(onStandardOutput: output.Add),
			new CommandOptions { WorkingDirectory = AbsoluteDirectoryPath.Create(directory) }).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, "Expected the command to run successfully.");

		// Comparing only the final segment keeps this robust where the temporary directory is
		// reached through a symlink, as it is on macOS, and the process reports the resolved path
		// rather than the one it was handed.
		Assert.AreEqual(
			Path.GetFileName(directory),
			Path.GetFileName(string.Concat(output).Trim()),
			"Expected the process to start in the directory it was given.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldInheritTheCurrentDirectoryWhenNoWorkingDirectoryIsGiven()
	{
		(string fileName, string[] arguments) = GetPrintWorkingDirectoryCommand();
		List<string> output = [];

		int exitCode = await RunCommand.ExecuteAsync(
			fileName,
			arguments,
			new LineOutputHandler(onStandardOutput: output.Add),
			new CommandOptions()).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, "Expected the command to run successfully.");
		Assert.AreEqual(
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.CurrentDirectory)),
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(string.Concat(output).Trim())),
			ignoreCase: RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
			"Expected an unset working directory to leave the previous behaviour untouched.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldThrowArgumentNullExceptionWhenOptionsAreNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => RunCommand.ExecuteAsync("dotnet", ["--version"], new OutputHandler(), null!)).ConfigureAwait(false);
	}
	[TestMethod]
	public async Task ExecuteAsyncShouldSetAnEnvironmentVariableForTheChildProcess()
	{
		string name = EnvironmentVariableNameFor();

		string reported = await ReadEnvironmentVariableFromChildAsync(
			name,
			new CommandOptions { EnvironmentVariables = new Dictionary<string, string?> { [name] = "expected" } }).ConfigureAwait(false);

		Assert.AreEqual("[expected]", reported, "Expected the child to see the variable that was set for it.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldOverrideAnInheritedEnvironmentVariable()
	{
		string name = EnvironmentVariableNameFor();
		Environment.SetEnvironmentVariable(name, "inherited");

		try
		{
			string reported = await ReadEnvironmentVariableFromChildAsync(
				name,
				new CommandOptions { EnvironmentVariables = new Dictionary<string, string?> { [name] = "override" } }).ConfigureAwait(false);

			Assert.AreEqual("[override]", reported, "Expected the overlay to win over the inherited value.");
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldRemoveAnInheritedEnvironmentVariableWhenTheValueIsNull()
	{
		string name = EnvironmentVariableNameFor();
		Environment.SetEnvironmentVariable(name, "inherited");

		try
		{
			string reported = await ReadEnvironmentVariableFromChildAsync(
				name,
				new CommandOptions { EnvironmentVariables = new Dictionary<string, string?> { [name] = null } }).ConfigureAwait(false);

			// An unset variable prints differently per shell -- cmd echoes the name back verbatim,
			// sh prints nothing -- so this pins the part that matters on both: the inherited value
			// did not reach the child.
			Assert.DoesNotContain("inherited", reported, "Expected a null value to remove the inherited variable.");
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldInheritTheEnvironmentWhenNoVariablesAreGiven()
	{
		string name = EnvironmentVariableNameFor();
		Environment.SetEnvironmentVariable(name, "inherited");

		try
		{
			string reported = await ReadEnvironmentVariableFromChildAsync(name, new CommandOptions()).ConfigureAwait(false);

			Assert.AreEqual("[inherited]", reported, "Expected an unset overlay to leave the previous behaviour untouched.");
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldRejectEnvironmentVariablesCombinedWithElevation()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Elevation only changes how the process is started on Windows.");
		}

		// Elevation forces UseShellExecute, which cannot carry an environment. Failing loudly beats
		// silently dropping variables the caller may be relying on.
		await Assert.ThrowsAsync<ArgumentException>(
			() => RunCommand.ExecuteAsync(
				"cmd",
				["/c", "exit 0"],
				new OutputHandler(),
				new CommandOptions
				{
					Elevation = Elevation.Elevated,
					EnvironmentVariables = new Dictionary<string, string?> { ["ANY"] = "value" },
				})).ConfigureAwait(false);
	}

	/// <summary>
	/// Returns a command that writes a file's bytes to standard output unchanged, as an executable
	/// plus separate arguments.
	/// </summary>
	/// <remarks>
	/// Windows has no reliably byte-faithful built-in for this. <c>cmd /c type</c> looked like one
	/// but transcodes a file carrying a UTF-16 byte order mark instead of copying it, which is
	/// exactly the input these tests need, so PowerShell writes the raw bytes to the standard
	/// output stream instead. <see cref="EmitsBytesFaithfully"/> checks the result rather than
	/// trusting it.
	/// </remarks>
	private static (string FileName, string[] Arguments) GetEmitFileBytesCommand(string path) =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("powershell", [
				"-NoProfile",
				"-NonInteractive",
				"-Command",
				$"$b=[IO.File]::ReadAllBytes('{path}'); $s=[Console]::OpenStandardOutput(); $s.Write($b,0,$b.Length); $s.Flush()"])
			: ("cat", [path]);

	private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

	// Runs the emitter through the library, so the test controls the exact bytes that reach the
	// pipe and can assert on what the library makes of them.
	private static string RunOverPath(string path, Encoding encoding)
	{
		StringBuilder output = new();
		(string fileName, string[] arguments) = GetEmitFileBytesCommand(path);
		_ = RunCommand.Execute(fileName, arguments, new OutputHandler(o => output.Append(o), null, encoding));

		return output.ToString();
	}

	/// <summary>
	/// Checks that this platform's emitter really does put <paramref name="bytes"/> on the pipe
	/// unchanged, so that a mangling emitter reports itself instead of being read as a result about
	/// the library.
	/// </summary>
	/// <remarks>
	/// This drives the emitter through <see cref="Process"/> directly and copies the raw
	/// <see cref="StreamReader.BaseStream"/>, deliberately never touching the code under test. A
	/// control that went through <see cref="RunCommand"/> would measure the very defect these tests
	/// exist to catch and blame the emitter for it, which would turn a regression into an
	/// inconclusive result instead of a failure.
	/// </remarks>
	private static bool EmitsBytesFaithfully(string path, byte[] bytes, out string diagnostic)
	{
		byte[] actual;

		try
		{
			(string fileName, string[] arguments) = GetEmitFileBytesCommand(path);
			ProcessStartInfo startInfo = new()
			{
				FileName = fileName,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			foreach (string argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}

			using Process process = Process.Start(startInfo)!;
			using MemoryStream captured = new();
			process.StandardOutput.BaseStream.CopyTo(captured);
			process.WaitForExit();
			actual = captured.ToArray();
		}
		catch (System.ComponentModel.Win32Exception ex)
		{
			diagnostic = $"This platform's byte emitter could not be started: {ex.Message}";
			return false;
		}

		bool faithful = actual.SequenceEqual(bytes);
		diagnostic = faithful
			? string.Empty
			: "This platform's byte emitter altered the bytes, so the library cannot be judged from them. "
				+ $"Expected [{Convert.ToHexString(bytes)}], the pipe carried [{Convert.ToHexString(actual)}].";

		return faithful;
	}

	// Writes the bytes to a file of this test's own and confirms the platform can actually put them
	// on a pipe unchanged, returning the path to feed to the library.
	private static string WriteBytesForTest(byte[] bytes, string caller)
	{
		string path = Path.Join(CreateDirectoryForTest(caller), "bytes.bin");
		File.WriteAllBytes(path, bytes);

		if (!EmitsBytesFaithfully(path, bytes, out string diagnostic))
		{
			Assert.Inconclusive(diagnostic);
		}

		return path;
	}

	private static void AssertDecodeFailure(byte[] bytes, [CallerMemberName] string caller = "")
	{
		// The emitter check has to happen out here: an Assert.Inconclusive raised inside the lambda
		// below would be caught by Assert.ThrowsExactly and reported as a failing test.
		string path = WriteBytesForTest(bytes, caller);

		AggregateException thrown = Assert.ThrowsExactly<AggregateException>(() => RunOverPath(path, StrictUtf8));
		Assert.IsTrue(
			thrown.Flatten().InnerExceptions.Any(e => e is DecoderFallbackException),
			$"Expected a decode failure, got: {thrown}");
	}

	[TestMethod]
	public void OutputEncodingIsNotReplacedByAUtf16ByteOrderMark() =>
		// "hi" in UTF-16LE behind its byte order mark. Process builds its StandardOutput reader with
		// byte-order-mark detection on, which used to switch the reader to UTF-16LE and return "hi"
		// even though the caller asked for strict UTF-8 and these are not valid UTF-8 bytes.
		AssertDecodeFailure([0xFF, 0xFE, (byte)'h', 0x00, (byte)'i', 0x00]);

	[TestMethod]
	public void OutputThatIsOnlyAUtf16ByteOrderMarkIsNotReportedAsSuccess() =>
		// The same detection consumed a lone FF FE as a byte order mark, leaving nothing to decode,
		// so a strict encoding reported no output and no error for bytes it should have rejected.
		AssertDecodeFailure([0xFF, 0xFE]);

	[TestMethod]
	public void AUtf8ByteOrderMarkIsStrippedFromTheStartOfOutput()
	{
		// A byte order mark matching the requested encoding is still dropped, so turning the
		// detection off did not start leaking U+FEFF into captured output.
		byte[] bytes = [0xEF, 0xBB, 0xBF, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o'];
		string path = WriteBytesForTest(bytes, nameof(AUtf8ByteOrderMarkIsStrippedFromTheStartOfOutput));

		Assert.AreEqual("hello", RunOverPath(path, StrictUtf8));
	}

	[TestMethod]
	public void ADecodeFailureIsNotDiscardedWhenTheProcessKeepsRunning()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs a shell that can emit bytes and then stay alive. The race this covers is in platform independent code, so the other legs cover it.");
		}

		// The read loop stops when the process exits, so a read that faults while the process is
		// still running used to be replaced by a fresh read on the next pass and its decode failure
		// thrown away. Sleeping after the bad byte keeps the process alive long enough for that pass
		// to happen, which makes the race deterministic rather than roughly one run in six.
		string path = Path.Join(CreateDirectoryForTest(), "bytes.bin");
		File.WriteAllBytes(path, [(byte)'o', (byte)'k', 0xFF]);

		StringBuilder output = new();
		AggregateException thrown = Assert.ThrowsExactly<AggregateException>(
			() => RunCommand.Execute(
				"sh",
				["-c", $"cat '{path}'; sleep 1"],
				new OutputHandler(o => output.Append(o), null, StrictUtf8)));

		Assert.IsTrue(
			thrown.Flatten().InnerExceptions.Any(e => e is DecoderFallbackException),
			$"Expected a decode failure, got: {thrown}");
	}

	/// <summary>
	/// Returns a command that reads one line from standard input and then reports what it read.
	/// </summary>
	/// <remarks>
	/// At end of stream the read fails and the variable stays empty, so the command still reaches its
	/// report and exits. That is the whole distinction being tested: with standard input closed the
	/// read ends immediately, and with it inherited from a handle nobody writes to the command waits
	/// there instead.
	/// </remarks>
	private static (string FileName, string[] Arguments) GetReadStandardInputCommand() =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("cmd", ["/c", "set \"line=\" & set /p line= & echo read:[%line%]"])
			: ("sh", ["-c", "read line; echo \"read:[$line]\""]);

	[TestMethod]
	public async Task ExecuteAsyncShouldEndACommandThatReadsStandardInputWhenStandardInputIsClosed()
	{
		// The token is the assertion. A command whose standard input is redirected but left open waits
		// on a pipe nobody writes to, exactly as one inheriting an idle handle does, so a regression
		// on either half ends this test by cancelling it rather than by hanging the suite.
		using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));

		StringBuilder output = new();
		(string fileName, string[] arguments) = GetReadStandardInputCommand();

		int exitCode = await RunCommand.ExecuteAsync(
			fileName,
			arguments,
			new OutputHandler(o => output.Append(o)),
			new CommandOptions { StandardInput = StandardInputMode.Closed },
			cancellation.Token).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, $"Expected the command to run to completion. Output: {output}");
		Assert.Contains("read:[]", output.ToString(), $"Expected the read to report end of stream. Output: {output}");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldGiveTheCommandItsOwnStandardInputWhenClosed()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			Assert.Inconclusive("Needs procfs to name a file descriptor. The redirection this covers is in platform independent code, so the other legs cover it.");
		}

		// Reaching end of stream does not by itself prove the command got the library's pipe. A caller
		// whose own standard input is already at end of stream — /dev/null under most test runners —
		// hands its child the same answer by inheritance, so a change that ignored the option
		// altogether would still look right there. Comparing the descriptors tells them apart:
		// redirection always makes a new pipe, so the command's standard input is whatever this
		// process has only when it was inherited.
		using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));

		string ownStandardInput = File.ResolveLinkTarget("/proc/self/fd/0", returnFinalTarget: false)?.FullName ?? "";
		Assert.AreNotEqual("", ownStandardInput, "Expected to be able to name this process's own standard input.");

		StringBuilder output = new();

		int exitCode = await RunCommand.ExecuteAsync(
			"sh",
			["-c", "readlink /proc/self/fd/0"],
			new OutputHandler(o => output.Append(o)),
			new CommandOptions { StandardInput = StandardInputMode.Closed },
			cancellation.Token).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode, $"Expected the probe to run successfully. Output: {output}");

		string commandStandardInput = output.ToString().Trim();
		Assert.AreNotEqual(
			ownStandardInput,
			commandStandardInput,
			"Expected the command's standard input to be the library's own pipe rather than this process's inherited handle.");
		Assert.StartsWith("pipe:", commandStandardInput, $"Expected the command's standard input to be a pipe, got: {commandStandardInput}");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldRejectClosedStandardInputCombinedWithElevation()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Elevation only changes how the process is started on Windows.");
		}

		// Elevation forces UseShellExecute, which has no stream to redirect. Failing loudly beats
		// starting a command whose standard input is still the caller's, which is the wait the option
		// was asked for to avoid.
		await Assert.ThrowsAsync<ArgumentException>(
			() => RunCommand.ExecuteAsync(
				"cmd",
				["/c", "exit 0"],
				new OutputHandler(),
				new CommandOptions
				{
					Elevation = Elevation.Elevated,
					StandardInput = StandardInputMode.Closed,
				})).ConfigureAwait(false);
	}
}
