// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RunCommand.Test;

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
}
