// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand.Test;

using System.Runtime.InteropServices;

[TestClass]
public class OverlayPathTests
{
	private static readonly string[] ExpectedOverlayCopy = ["ran overlay copy"];
	private static readonly string[] ExpectedStarted = ["started"];

	[TestMethod]
	public async Task ExecuteAsyncShouldStartAToolThatOnlyTheOverlayPathHolds()
	{
		SkipOnWindows();
		string directory = RunCommandTests.CreateDirectoryForTest();
		string name = $"runcommand-{nameof(ExecuteAsyncShouldStartAToolThatOnlyTheOverlayPathHolds)}";
		WriteScript(directory, name, "echo ran overlay copy", executable: true);

		List<string> output = await RunOnOverlayPathAsync(name, directory).ConfigureAwait(false);

		Assert.AreSequenceEqual(ExpectedOverlayCopy, output, "Expected the tool on the overlay PATH to start.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldPreferTheOverlayPathOverTheCallersPath()
	{
		SkipOnWindows();

		// echo is on the caller's PATH too, so the old lookup would have started that one and printed the argument.
		string directory = RunCommandTests.CreateDirectoryForTest();
		WriteScript(directory, "echo", "printf 'ran overlay copy\\n'", executable: true);

		List<string> output = await RunOnOverlayPathAsync("echo", directory, "ran caller copy").ConfigureAwait(false);

		Assert.AreSequenceEqual(ExpectedOverlayCopy, output, "Expected the overlay PATH to choose which echo runs.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldSkipANonExecutableFileOnTheOverlayPath()
	{
		SkipOnWindows();
		string root = RunCommandTests.CreateDirectoryForTest();
		string notExecutable = Directory.CreateDirectory(Path.Join(root, "a")).FullName;
		string executable = Directory.CreateDirectory(Path.Join(root, "b")).FullName;
		string name = $"runcommand-{nameof(ExecuteAsyncShouldSkipANonExecutableFileOnTheOverlayPath)}";
		WriteScript(notExecutable, name, "echo ran the file that cannot run", executable: false);
		WriteScript(executable, name, "echo ran overlay copy", executable: true);

		List<string> output = await RunOnOverlayPathAsync(name, $"{notExecutable}{Path.PathSeparator}{executable}").ConfigureAwait(false);

		Assert.AreSequenceEqual(ExpectedOverlayCopy, output, "Expected the search to pass over a file without an execute bit, as a shell does.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldSkipAFileOnTheOverlayPathThatOnlyOthersMayExecute()
	{
		SkipOnWindows();
		if (Environment.UserName == "root")
		{
			Assert.Inconclusive("root may execute any file that has an execute bit, so the owner's missing bit is not visible.");
		}

		// Group and others may execute this file but its owner, the caller, may not, so a shell passes over it.
		// Testing for any execute bit accepted it and the start failed with "Permission denied" (#139).
		string root = RunCommandTests.CreateDirectoryForTest();
		string othersOnly = Directory.CreateDirectory(Path.Join(root, "a")).FullName;
		string executable = Directory.CreateDirectory(Path.Join(root, "b")).FullName;
		string name = $"runcommand-{nameof(ExecuteAsyncShouldSkipAFileOnTheOverlayPathThatOnlyOthersMayExecute)}";
		WriteScript(othersOnly, name, "echo ran the file the caller cannot run", executable: false);
		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(
				Path.Join(othersOnly, name),
				UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
		}

		WriteScript(executable, name, "echo ran overlay copy", executable: true);

		List<string> output = await RunOnOverlayPathAsync(name, $"{othersOnly}{Path.PathSeparator}{executable}").ConfigureAwait(false);

		Assert.AreSequenceEqual(ExpectedOverlayCopy, output, "Expected the search to ask whether the caller may execute the file, as a shell does.");
	}

	[TestMethod]
	public void CanExecuteShouldReportWhetherTheCallerMayExecuteTheFile()
	{
		SkipOnWindows();
		string directory = RunCommandTests.CreateDirectoryForTest();
		WriteScript(directory, "runs", "exit 0", executable: true);
		WriteScript(directory, "does-not-run", "exit 0", executable: false);

		Assert.IsTrue(NativeMethods.CanExecute(Path.Join(directory, "runs")));
		Assert.IsFalse(NativeMethods.CanExecute(Path.Join(directory, "does-not-run")));
	}

	[TestMethod]
	public void CanExecuteShouldKeepTheFileWhenTheCLibraryCannotBeCalled()
	{
		// Without access(), the search keeps every existing file, as it did before it checked permissions.
		Assert.IsTrue(NativeMethods.CanExecute("/any/file", (_, _) => throw new DllNotFoundException("libc")));
		Assert.IsTrue(NativeMethods.CanExecute("/any/file", (_, _) => throw new EntryPointNotFoundException("access")));
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldFindTheToolOnTheCallersPathWhenTheOverlayRemovesPath()
	{
		SkipOnWindows();
		List<string> output = [];

		int exitCode = await RunCommand.ExecuteAsync(
			"sh",
			["-c", "echo started"],
			new LineOutputHandler(onStandardOutput: output.Add),
			new CommandOptions { EnvironmentVariables = new Dictionary<string, string?> { ["PATH"] = null } }).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode);
		Assert.AreSequenceEqual(ExpectedStarted, output, "Expected a removed PATH to leave the usual lookup in place.");
	}

	[TestMethod]
	public void FindOverlayPathShouldMatchTheKeyIgnoringCaseOnlyOnWindows()
	{
		Dictionary<string, string?> overlay = new(StringComparer.Ordinal) { ["Path"] = "/pinned" };

		Assert.AreEqual("/pinned", RunCommand.FindOverlayPath(overlay, isWindows: true));
		Assert.IsNull(RunCommand.FindOverlayPath(overlay, isWindows: false));
	}

	[TestMethod]
	public void ResolveOnSearchPathShouldAddExeToANameWithNoExtensionOnWindows()
	{
		string directory = RunCommandTests.CreateDirectoryForTest();
		string name = $"tool-{nameof(ResolveOnSearchPathShouldAddExeToANameWithNoExtensionOnWindows)}";
		WriteScript(directory, $"{name}.exe", "exit 0", executable: true);

		string? resolved = RunCommand.ResolveOnSearchPath(name, directory, isWindows: true);

		Assert.AreEqual(Path.Join(directory, $"{name}.exe"), resolved);
	}

	[TestMethod]
	public void ResolveOnSearchPathShouldNeverResolveABareNameToABatchFileOnWindows()
	{
		// cmd.exe re-parses a batch file's command line, so a bare name that reached tool.cmd would let & or | in an
		// argument run a second command (ktsu-dev/RunCommand#138). CreateProcess only ever adds .exe to such a name.
		string directory = RunCommandTests.CreateDirectoryForTest();
		string name = $"tool-{nameof(ResolveOnSearchPathShouldNeverResolveABareNameToABatchFileOnWindows)}";
		WriteScript(directory, $"{name}.cmd", "exit 0", executable: true);
		WriteScript(directory, $"{name}.bat", "exit 0", executable: true);
		WriteScript(directory, $"{name}.com", "exit 0", executable: true);

		Assert.IsNull(RunCommand.ResolveOnSearchPath(name, directory, isWindows: true));
		Assert.AreEqual(Path.Join(directory, $"{name}.cmd"), RunCommand.ResolveOnSearchPath($"{name}.cmd", directory, isWindows: true), "A batch file named with its extension is the caller's explicit choice.");
	}

	[TestMethod]
	public void ResolveOnSearchPathShouldLeaveANameWithADirectoryPartAlone()
	{
		string directory = RunCommandTests.CreateDirectoryForTest();
		WriteScript(directory, "tool", "exit 0", executable: true);

		Assert.IsNull(RunCommand.ResolveOnSearchPath("./tool", directory, isWindows: false));
		Assert.IsNull(RunCommand.ResolveOnSearchPath(@"bin\tool", directory, isWindows: true));
		Assert.IsNull(RunCommand.ResolveOnSearchPath("C:tool", directory, isWindows: true));
	}

	[TestMethod]
	public void ResolveOnSearchPathShouldSkipRelativeAndMissingEntries()
	{
		string directory = RunCommandTests.CreateDirectoryForTest();
		string name = $"tool-{nameof(ResolveOnSearchPathShouldSkipRelativeAndMissingEntries)}";
		WriteScript(directory, name, "exit 0", executable: true);
		string searchPath = string.Join(Path.PathSeparator, "relative", Path.Join(directory, "missing"), "", directory);

		Assert.AreEqual(Path.Join(directory, name), RunCommand.ResolveOnSearchPath(name, searchPath, isWindows: false));
		Assert.IsNull(RunCommand.ResolveOnSearchPath(name, "relative", isWindows: false));
	}

	internal static void SkipOnWindows()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Uses shell scripts and execute permission bits. The lookup this covers is shared, and the Unix legs cover it.");
		}
	}

	private static void WriteScript(string directory, string name, string body, bool executable)
	{
		string path = Path.Join(directory, name);
		File.WriteAllText(path, $"#!/bin/sh\n{body}\n");
		if (!OperatingSystem.IsWindows())
		{
			UnixFileMode mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
			File.SetUnixFileMode(path, executable ? mode | UnixFileMode.UserExecute : mode);
		}
	}

	private static async Task<List<string>> RunOnOverlayPathAsync(string fileName, string overlayDirectories, params string[] arguments)
	{
		List<string> output = [];
		int exitCode = await RunCommand.ExecuteAsync(
			fileName,
			arguments,
			new LineOutputHandler(onStandardOutput: output.Add),
			new CommandOptions { EnvironmentVariables = new Dictionary<string, string?> { ["PATH"] = $"{overlayDirectories}{Path.PathSeparator}/usr/bin{Path.PathSeparator}/bin" } }).ConfigureAwait(false);
		Assert.AreEqual(0, exitCode);
		return output;
	}
}
