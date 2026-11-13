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
	private static readonly string[] ExpectedDone = ["done"];

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
	internal static string CreateDirectoryForTest([CallerMemberName] string caller = "") =>
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
	public void CancelShouldNotThrowWhenTheTreeKillCannotTerminateEveryDescendant()
	{
		(string fileName, string[] arguments) = GetSleepCommand();
		using Process process = Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false })!;

		try
		{
			// What Process.Kill(entireProcessTree: true) throws when a descendant belongs to another
			// user or runs elevated.
			static void PartlyFailingKill(Process _) =>
				throw new AggregateException(
					"Not all processes in the process tree could be terminated.",
					new System.ComponentModel.Win32Exception(1, "Operation not permitted"));

			// Registered the way RunAsync registers its kill, so this is the caller's Cancel().
			using CancellationTokenSource cancellationTokenSource = new();
			using CancellationTokenRegistration registration = cancellationTokenSource.Token.Register(
				() => RunCommand.TryKill(process, PartlyFailingKill));

			cancellationTokenSource.Cancel();
			Assert.IsTrue(cancellationTokenSource.IsCancellationRequested, "Expected Cancel() to return normally after the kill failed.");

			// Called directly as well, as the catch blocks in RunAsync do: an exception here would
			// replace the OperationCanceledException or the handler failure being reported.
			RunCommand.TryKill(process, PartlyFailingKill);

			// Only the injected kill ran, so the process is still alive for the finally block to end.
			Assert.IsFalse(process.HasExited, "Expected only the failing kill to have run.");
		}
		finally
		{
			process.Kill(entireProcessTree: true);
		}
	}

	[TestMethod]
	public void KillEntireTreeWhereSupportedShouldEndTheGrandchildrenToo()
	{
		OverlayPathTests.SkipOnWindows();

		// The netstandard builds, which .NET Core 3.x and .NET 5–7 apps load, kill through this method. Killing only
		// the shell would leave its background sleep running (#134).
		using Process process = Process.Start(new ProcessStartInfo("sh", ["-c", "sleep 300 & echo $!; wait"])
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
		})!;
		int grandchild = int.Parse(process.StandardOutput.ReadLine()!, System.Globalization.CultureInfo.InvariantCulture);

		try
		{
			RunCommand.KillEntireTreeWhereSupported(process);
			process.WaitForExit();

			bool ended = SpinWait.SpinUntil(() => !IsRunning(grandchild), TimeSpan.FromSeconds(5));

			Assert.IsTrue(ended, "Expected the shell's background sleep to be killed with it.");
		}
		finally
		{
			if (IsRunning(grandchild))
			{
				using Process leftover = Process.GetProcessById(grandchild);
				leftover.Kill();
			}
		}

		static bool IsRunning(int processId)
		{
			try
			{
				using Process candidate = Process.GetProcessById(processId);
				return !candidate.HasExited;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}
	}

	[TestMethod]
	public void KillEntireTreeWhereSupportedShouldKillTheProcessAloneWithoutTheTreeOverload()
	{
		// .NET Framework and Mono have no Process.Kill(bool), so the process itself is still killed there.
		(string fileName, string[] arguments) = GetSleepCommand();
		using Process process = Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false })!;

		try
		{
			RunCommand.KillEntireTreeWhereSupported(process, killEntireProcessTree: null);

			Assert.IsTrue(process.WaitForExit(10_000), "Expected the process to be killed without the tree overload.");
		}
		finally
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}
		}
	}

	[TestMethod]
	public void KillEntireTreeWhereSupportedShouldThrowTheOverloadsOwnExceptionUnwrapped()
	{
		// TryKill catches InvalidOperationException, Win32Exception and AggregateException by type, so a
		// TargetInvocationException from the reflection call would escape it (#107).
		using Process neverStarted = new();

		_ = Assert.ThrowsExactly<InvalidOperationException>(() => RunCommand.KillEntireTreeWhereSupported(neverStarted));
	}

	[TestMethod]
	public async Task CancelShouldNotRunTheCallersContinuationInline()
	{
		using CancellationTokenSource cancellationTokenSource = new();
		(string fileName, string[] arguments) = GetSleepCommand();
		bool insideCancel = false;
		bool continuationRanInsideCancel = false;

		Task execution = Task.Run(async () =>
		{
			try
			{
				_ = await RunCommand.ExecuteAsync(fileName, arguments, new OutputHandler(), cancellationTokenSource.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				continuationRanInsideCancel = Volatile.Read(ref insideCancel);
			}
		});

		// Give the command time to start, so the cancellation reaches a run that is reading output.
		await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

		Volatile.Write(ref insideCancel, true);
		CancelSynchronously(cancellationTokenSource);
		Volatile.Write(ref insideCancel, false);

		Task finished = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
		Assert.AreSame(execution, finished, "Expected the cancelled call to end.");
		Assert.IsFalse(continuationRanInsideCancel, "Expected the caller's catch block to run after Cancel() returned, not inside it.");
	}

	[TestMethod]
	public async Task CancelWhileHoldingALockTheCallersCleanupNeedsShouldNotDeadlock()
	{
		using CancellationTokenSource cancellationTokenSource = new();
		using SemaphoreSlim gate = new(1, 1);
		(string fileName, string[] arguments) = GetSleepCommand();
		bool cleanupAcquiredTheLock = false;

		Task execution = Task.Run(async () =>
		{
			try
			{
				_ = await RunCommand.ExecuteAsync(fileName, arguments, new OutputHandler(), cancellationTokenSource.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Bounded so that, before the fix, this reports a failure instead of hanging the run:
				// the cleanup ran inside Cancel(), on the thread that holds the gate.
				cleanupAcquiredTheLock = WaitSynchronously(gate, TimeSpan.FromSeconds(5));
				if (cleanupAcquiredTheLock)
				{
					_ = gate.Release();
				}
			}
		});

		await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

		await gate.WaitAsync().ConfigureAwait(false);
		Stopwatch cancelTime = Stopwatch.StartNew();
		try
		{
			CancelSynchronously(cancellationTokenSource);
		}
		finally
		{
			cancelTime.Stop();
			_ = gate.Release();
		}

		Task finished = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(15))).ConfigureAwait(false);
		Assert.AreSame(execution, finished, "Expected the cancelled call to end.");
		Assert.IsTrue(cleanupAcquiredTheLock, $"Expected the caller's cleanup to acquire the lock once Cancel() released it; Cancel() took {cancelTime.ElapsedMilliseconds} ms.");
	}

	// Cancel() rather than CancelAsync(), because the difference under test is what runs on the
	// cancelling thread before Cancel() returns.
	private static void CancelSynchronously(CancellationTokenSource cancellationTokenSource) =>
		cancellationTokenSource.Cancel();

	private static bool WaitSynchronously(SemaphoreSlim semaphore, TimeSpan timeout) =>
		semaphore.Wait(timeout);

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
	public async Task ExecuteAsyncShouldKillTheProcessTreeEveryTimeItIsCancelled()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			Assert.Inconclusive("Reads a descendant's state from /proc. The kill this covers is in platform independent code, so the Linux leg covers it.");
		}

		// A token runs its callbacks newest first, and the wait registers its own after the kill's.
		// So the wait can observe cancellation, end the call, and dispose the kill's registration
		// before that callback has run, and the call returns with its whole tree still running. The
		// wait only wins that race some of the time, which is why this repeats. The shape is a
		// caller's own deadline linked into its token, which cancels on a timer thread.
		for (int attempt = 0; attempt < 40; attempt++)
		{
			TaskCompletionSource<int> descendant = new(TaskCreationOptions.RunContinuationsAsynchronously);
			OutputHandler handler = new(
				chunk =>
				{
					if (int.TryParse(chunk.Trim(), out int id))
					{
						descendant.TrySetResult(id);
					}
				},
				_ => { });

			using CancellationTokenSource timeout = new(TimeSpan.FromMilliseconds(200));
			using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None, timeout.Token);

			Task<int> execution = RunCommand.ExecuteAsync(
				"sh",
				["-c", "sleep 30 & echo $!; wait"],
				handler,
				new CommandOptions { StandardInput = StandardInputMode.Closed },
				linked.Token);

			int descendantId = await descendant.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

			await Assert.ThrowsAsync<OperationCanceledException>(() => execution).ConfigureAwait(false);

			Assert.IsTrue(
				await HasExitedAsync(descendantId).ConfigureAwait(false),
				$"Attempt {attempt}: descendant {descendantId} was still running after its call was cancelled.");
		}
	}

	/// <summary>
	/// Waits up to five seconds for a process to exit.
	/// </summary>
	/// <remarks>
	/// Reads <c>/proc</c> rather than asking <see cref="Process"/>, because the process is not a child
	/// of this one: once its parent is killed it is reparented, and it can sit as a zombie until its
	/// new parent reaps it. A zombie has exited, so it counts as gone.
	/// </remarks>
	private static async Task<bool> HasExitedAsync(int processId)
	{
		for (int check = 0; check < 50; check++)
		{
			string status;

			try
			{
				status = await File.ReadAllTextAsync($"/proc/{processId}/stat").ConfigureAwait(false);
			}
			catch (IOException)
			{
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				return true;
			}

			// The state follows the parenthesised command name, which may itself contain spaces.
			if (status[(status.LastIndexOf(')') + 2)..].StartsWith('Z'))
			{
				return true;
			}

			await Task.Delay(100).ConfigureAwait(false);
		}

		return false;
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
	[DataRow("")]
	[DataRow("sub")]
	public async Task ExecuteAsyncShouldResolveARelativeExecutablePathAgainstTheWorkingDirectory(string subdirectory)
	{
		// A same-named script sits in the caller's current directory too, so running the wrong copy
		// shows up in the output rather than as a start failure
		bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		string scriptName = $"{nameof(RunCommandTests)}-{Guid.NewGuid():N}{(isWindows ? ".cmd" : ".sh")}";
		string relativePath = string.IsNullOrEmpty(subdirectory) ? $".{Path.DirectorySeparatorChar}{scriptName}" : $"{subdirectory}/{scriptName}";

		string workingDirectory = CreateDirectoryForTest(nameof(ExecuteAsyncShouldResolveARelativeExecutablePathAgainstTheWorkingDirectory) + subdirectory);
		string intendedScript = Path.Join(workingDirectory, subdirectory, scriptName);
		string callerScript = Path.Join(Environment.CurrentDirectory, subdirectory, scriptName);
		_ = Directory.CreateDirectory(Path.GetDirectoryName(intendedScript)!);
		_ = Directory.CreateDirectory(Path.GetDirectoryName(callerScript)!);
		WriteScript(intendedScript, "working-directory");
		WriteScript(callerScript, "caller");

		try
		{
			List<string> output = [];
			int exitCode = await RunCommand.ExecuteAsync(
				relativePath,
				[],
				new LineOutputHandler(onStandardOutput: output.Add),
				new CommandOptions { WorkingDirectory = AbsoluteDirectoryPath.Create(workingDirectory) }).ConfigureAwait(false);

			Assert.AreEqual(0, exitCode, "Expected the script to run successfully.");
			Assert.AreEqual("working-directory", string.Concat(output).Trim(), "Expected the script in the working directory to run, not the caller's copy.");
		}
		finally
		{
			File.Delete(callerScript);
			File.Delete(intendedScript);
		}
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldResolveDotDotFromWhereASymlinkedWorkingDirectoryPoints()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Windows resolves '..' as text, as the shell there does, so there is no physical parent to follow.");
		}

		// <root>/link points at <root>/real/sub. From there a shell's '../tool.sh' is <root>/real/tool.sh,
		// and a decoy at <root>/tool.sh shows up if '..' is collapsed as text against the link instead.
		string root = CreateDirectoryForTest();
		string real = Path.Join(root, "real");
		string target = Path.Join(real, "sub");
		string link = Path.Join(root, "link");
		string scriptName = $"{nameof(RunCommandTests)}-{Guid.NewGuid():N}.sh";
		string intendedScript = Path.Join(real, scriptName);
		string decoyScript = Path.Join(root, scriptName);
		_ = Directory.CreateDirectory(target);
		if (Directory.Exists(link) || File.Exists(link))
		{
			File.Delete(link);
		}

		try
		{
			_ = Directory.CreateSymbolicLink(link, target);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			Assert.Inconclusive($"Symbolic links cannot be created here: {exception.Message}");
		}

		WriteScript(intendedScript, "physical-parent");
		WriteScript(decoyScript, "lexical-parent");

		try
		{
			List<string> output = [];
			int exitCode = await RunCommand.ExecuteAsync(
				$"../{scriptName}",
				[],
				new LineOutputHandler(onStandardOutput: output.Add),
				new CommandOptions { WorkingDirectory = AbsoluteDirectoryPath.Create(link) }).ConfigureAwait(false);

			Assert.AreEqual(0, exitCode, "Expected the script to run successfully.");
			Assert.AreEqual("physical-parent", string.Concat(output).Trim(), "Expected '..' to name the parent of the directory the link points to, as 'cd link && ../tool.sh' does.");
		}
		finally
		{
			File.Delete(link);
			File.Delete(intendedScript);
			File.Delete(decoyScript);
		}
	}

	private static void WriteScript(string path, string message)
	{
		if (OperatingSystem.IsWindows())
		{
			File.WriteAllText(path, $"@echo {message}\r\n");
			return;
		}

		File.WriteAllText(path, $"#!/bin/sh\necho {message}\n");
		File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
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
	/// Returns a command that reads one line from standard input and then reports what it read.
	/// </summary>
	/// <remarks>
	/// At end of stream the read fails and the variable stays empty, so the command still reaches its
	/// report and exits. That is the whole distinction being tested: with standard input closed the
	/// read ends immediately, and with it inherited from a handle nobody writes to the command waits
	/// there instead.
	/// <para>
	/// The Windows arm reports the read through control flow rather than by echoing the variable,
	/// because neither kind of expansion survives this command line. <c>%line%</c> comes back
	/// literal — expanding an undefined variable to nothing is batch-file behaviour, not
	/// command-line behaviour — and <c>!line!</c> under <c>/v:on</c> came back literal too, so both
	/// reported <c>read:[%line%]</c> and <c>read:[!line!]</c> instead of saying what the read did.
	/// <c>if defined</c> is a run-time test on the name, so it needs no expansion at all, and every
	/// string that reaches standard output is a literal. <c>set line=</c> first, so a variable
	/// inherited from the environment cannot make the command report a read that never happened.
	/// The prompt is left empty by putting nothing between <c>=</c> and the separator, so no prompt
	/// text reaches the captured output.
	/// </para>
	/// </remarks>
	private static (string FileName, string[] Arguments) GetReadStandardInputCommand() =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("cmd", ["/c", "set line=&set /p line=&if defined line (echo read:[unexpected]) else (echo read:[])"])
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

	[TestMethod]
	public async Task ExecuteAsyncShouldDeliverOutputStillInThePipeWhenTheProcessExits()
	{
		// Far more than one 4096-character read and more than a Linux pipe's 64 KB, from a command
		// that exits as soon as it has written it, so most of the output is still unread at exit.
		const int length = 200_000;
		string path = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(path, [.. Enumerable.Repeat((byte)'a', length)]).ConfigureAwait(false);
			(string fileName, string[] arguments) = OutputDecodingTests.GetEmitFileBytesCommand(path);

			// The loss depends on how far the reads have got when the process exits, so one clean
			// run proves little. Repeat it.
			StringBuilder output = new();
			for (int attempt = 0; attempt < 10; attempt++)
			{
				lock (output)
				{
					output.Clear();
				}

				int exitCode = await RunCommand.ExecuteAsync(
					fileName,
					arguments,
					new OutputHandler(o =>
					{
						lock (output)
						{
							output.Append(o);
						}
					}),
					new CommandOptions()).ConfigureAwait(false);

				Assert.AreEqual(0, exitCode, "Expected exit code to be 0 for successful command.");
				Assert.AreEqual(length, output.Length, $"Output was truncated on attempt {attempt + 1}.");
			}
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldNeverRunTheStandardOutputAndStandardErrorDelegatesAtOnce()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs a shell that interleaves many lines across both streams. The delivery this covers is in platform independent code, so the other legs cover it.");
		}

		// Both delegates add to one List<T>, which is not thread safe, exactly as a caller capturing
		// a command's whole output would. Run concurrently, overlapping adds overwrite each other's
		// slots and lines vanish without an exception.
		const int linesPerStream = 5000;

		// Whether the delegates overlap depends on how the two reads are scheduled, so one clean
		// run proves little. Repeat it.
		for (int attempt = 0; attempt < 5; attempt++)
		{
			List<string> lines = [];
			int active = 0;
			int overlaps = 0;

			void Add(string line)
			{
				if (Interlocked.Increment(ref active) > 1)
				{
					_ = Interlocked.Increment(ref overlaps);
				}

				lines.Add(line);
				Thread.SpinWait(50);
				_ = Interlocked.Decrement(ref active);
			}

			int exitCode = await RunCommand.ExecuteAsync(
				"sh",
				["-c", $"i=1; while [ $i -le {linesPerStream} ]; do echo out$i; echo err$i >&2; i=$((i+1)); done"],
				new LineOutputHandler(Add, Add)).ConfigureAwait(false);

			Assert.AreEqual(0, exitCode);
			Assert.AreEqual(0, overlaps, $"The delegates ran inside each other {overlaps} times on attempt {attempt + 1}.");
			Assert.HasCount(linesPerStream * 2, lines, $"Lines were lost on attempt {attempt + 1}.");

			// Serialising delivery must not reorder either stream.
			string[] expectedOut = [.. Enumerable.Range(1, linesPerStream).Select(i => $"out{i}")];
			string[] expectedErr = [.. Enumerable.Range(1, linesPerStream).Select(i => $"err{i}")];
			Assert.AreSequenceEqual(expectedOut, [.. lines.Where(l => l.StartsWith("out", StringComparison.Ordinal))]);
			Assert.AreSequenceEqual(expectedErr, [.. lines.Where(l => l.StartsWith("err", StringComparison.Ordinal))]);
		}
	}

	[TestMethod]
	[DoNotParallelize]
	public async Task ExecuteAsyncShouldNotSpinWhileACommandThatClosedItsOutputKeepsRunning()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs a shell that can close its own output streams and stay alive. The read loop this covers is in platform independent code, so the other legs cover it.");
		}

		// Standard error reaches end of stream at once while the command runs on. A reader that
		// keeps reading a stream after end of stream gets 0 back straight away every time, so it
		// spins on a core for the whole run instead of waiting.
		const int runMilliseconds = 2000;
		List<string> output = [];

		using Process self = Process.GetCurrentProcess();
		TimeSpan before = self.TotalProcessorTime;

		int exitCode = await RunCommand.ExecuteAsync(
			"sh",
			["-c", $"exec 2>&-; sleep {runMilliseconds / 1000}; echo done"],
			new LineOutputHandler(onStandardOutput: line =>
			{
				lock (output)
				{
					output.Add(line);
				}
			})).ConfigureAwait(false);

		self.Refresh();
		TimeSpan used = self.TotalProcessorTime - before;

		Assert.AreEqual(0, exitCode);
		Assert.AreSequenceEqual(ExpectedDone, output, "Expected standard output to be delivered in full.");

		// This is process-wide CPU, which is why the test opts out of running alongside the others.
		// A spinning reader costs at least a full core for the whole run, so half of the run is
		// clear of the test host's own background work and still well short of a spin.
		Assert.IsLessThan(
			runMilliseconds / 2,
			used.TotalMilliseconds,
			$"Expected the reader to wait rather than spin, but the process used {used.TotalMilliseconds:F0} ms of CPU during a {runMilliseconds} ms run.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldThrowAndKillTheCommandWhenTheOutputHandlerThrows() =>
		await AssertAFaultedReadEndsTheCall(
			"echo first",
			new OutputHandler(_ => throw new InvalidOperationException("handler failed"))).ConfigureAwait(false);

	[TestMethod]
	public async Task ExecuteAsyncShouldThrowAndKillTheCommandWhenAStrictEncodingRejectsTheOutput() =>
		await AssertAFaultedReadEndsTheCall(
			@"printf '\377\n'",
			new OutputHandler(encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true))).ConfigureAwait(false);

	[TestMethod]
	public async Task ExecuteAsyncShouldThrowAndKillTheCommandWhenTheOutputHandlerThrowsOperationCanceled() =>
		await AssertAFaultedReadEndsTheCall(
			"echo first",
			new OutputHandler(_ => throw new OperationCanceledException(HandlerCancellationMessage)),
			expectHandlerCancellation: true).ConfigureAwait(false);

	[TestMethod]
	public async Task ExecuteAsyncShouldThrowAndKillTheCommandWhenALineOutputHandlerThrowsTaskCanceled() =>
		await AssertAFaultedReadEndsTheCall(
			"echo first",
			new LineOutputHandler(_ => throw new TaskCanceledException(HandlerCancellationMessage)),
			expectHandlerCancellation: true).ConfigureAwait(false);

	[TestMethod]
	public async Task ExecuteAsyncShouldRethrowAnOperationCanceledExceptionFromTheOutputHandlerWhenTheOutputIsSmall()
	{
		List<string> delivered = [];
		OperationCanceledException thrown = await Assert.ThrowsAsync<OperationCanceledException>(
			() => RunCommand.ExecuteAsync(
				"sh",
				["-c", "echo one; sleep 0.2; echo two"],
				new OutputHandler(chunk =>
				{
					delivered.Add(chunk);
					throw new OperationCanceledException(HandlerCancellationMessage);
				}))).ConfigureAwait(false);

		Assert.AreEqual(HandlerCancellationMessage, thrown.Message, "Expected the handler's own exception, not one standing in for it.");
		Assert.HasCount(1, delivered, "Expected delivery to stop at the chunk whose handler threw.");
	}

	private const string HandlerCancellationMessage = "thrown by the output handler";

	/// <summary>
	/// Runs a command that makes the first read fault and then writes far more than a pipe holds,
	/// and requires the call to rethrow that fault promptly with the command no longer running.
	/// </summary>
	/// <remarks>
	/// A faulted read stops draining its pipe, so the command blocks writing to it and never exits.
	/// Waiting for that exit is what hung the call.
	/// </remarks>
	/// <param name="faultingOutput">Shell that produces the output whose read fails.</param>
	/// <param name="handler">The handler the command's output goes to.</param>
	/// <param name="expectHandlerCancellation">
	/// Whether the handler fails by throwing <see cref="OperationCanceledException"/> with
	/// <see cref="HandlerCancellationMessage"/>. That ends its read Canceled rather than Faulted, and
	/// the call has to rethrow it as itself.
	/// </param>
	/// <param name="testName">Names the pid file.</param>
	private static async Task AssertAFaultedReadEndsTheCall(string faultingOutput, OutputHandler handler, bool expectHandlerCancellation = false, [CallerMemberName] string testName = "")
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs procfs to tell whether the command is still running. The fault handling this covers is in platform independent code, so the other legs cover it.");
		}

		string pidFile = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)}.{testName}.pid");
		File.Delete(pidFile);

		try
		{
			// exec keeps the writer on the pid written to the file, so the check below looks at the
			// very process that is blocked on the full pipe.
			Task<int> execution = RunCommand.ExecuteAsync(
				"sh",
				["-c", $"echo $$ > '{pidFile}'; {faultingOutput}; sleep 0.5; exec head -c 1000000 /dev/zero"],
				handler);

			// Bounded rather than a bare await: before the fix this call never returns.
			Task finished = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);

			Assert.AreSame(execution, finished, "Expected a faulted read to end the call rather than wait on a command blocked writing to it.");
			if (expectHandlerCancellation)
			{
				Assert.IsTrue(execution.IsCanceled, $"Expected the handler's cancellation to reach the caller, but the call {execution.Status}.");
				OperationCanceledException thrown = await Assert.ThrowsAsync<OperationCanceledException>(() => execution).ConfigureAwait(false);
				Assert.AreEqual(HandlerCancellationMessage, thrown.Message, "Expected the handler's own exception, not one standing in for it.");
			}
			else
			{
				Assert.IsTrue(execution.IsFaulted, "Expected the fault to reach the caller.");
				Assert.IsNotInstanceOfType<OperationCanceledException>(execution.Exception!.InnerException);
			}

			string pid = (await File.ReadAllTextAsync(pidFile).ConfigureAwait(false)).Trim();
			Assert.IsFalse(IsRunning(pid), $"Expected the command (pid {pid}) to have been killed.");
		}
		finally
		{
			File.Delete(pidFile);
		}
	}

	/// <summary>
	/// Reports whether a process is still running, counting a zombie as gone since it has exited.
	/// </summary>
	private static bool IsRunning(string pid)
	{
		string statPath = $"/proc/{pid}/stat";
		if (!File.Exists(statPath))
		{
			return false;
		}

		try
		{
			string stat = File.ReadAllText(statPath);
			// The state follows the parenthesised command name, which may itself contain spaces.
			return stat[stat.LastIndexOf(')') + 2] != 'Z';
		}
		catch (IOException)
		{
			return false;
		}
	}
}
