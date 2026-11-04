// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
#if NETSTANDARD2_0
// Only the netstandard2.0 argument-escaping fallback needs these.
using System.Linq;
using System.Text;
#endif

/// <summary>
/// Provides functionality to execute shell commands and process their outputs.
/// </summary>
public static class RunCommand
{
	/// <summary>
	/// Executes a shell command synchronously.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <returns>The exit code of the executed process.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static int Execute(string command) =>
		ExecuteAsync(command).Result;

	/// <summary>
	/// Executes a shell command synchronously with an output handler.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <returns>The exit code of the executed process.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static int Execute(string command, OutputHandler outputHandler) =>
		ExecuteAsync(command, outputHandler).Result;

	/// <summary>
	/// Executes a shell command synchronously at the specified elevation level.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <returns>The exit code of the executed process.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static int Execute(string command, Elevation elevation) =>
		ExecuteAsync(command, elevation).Result;

	/// <summary>
	/// Executes a shell command synchronously with an output handler at the specified elevation level.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">
	/// The handler for processing command output. Not invoked when <paramref name="elevation"/> is
	/// <see cref="Elevation.Elevated"/> on Windows because elevation requires <c>UseShellExecute</c>,
	/// which is incompatible with output redirection.
	/// </param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <returns>The exit code of the executed process.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static int Execute(string command, OutputHandler outputHandler, Elevation elevation) =>
		ExecuteAsync(command, outputHandler, elevation).Result;

	/// <summary>
	/// Executes a shell command synchronously, passing arguments individually so that no manual
	/// quoting is required.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <returns>The exit code of the executed process.</returns>
	public static int Execute(string fileName, IEnumerable<string> arguments) =>
		ExecuteAsync(fileName, arguments).Result;

	/// <summary>
	/// Executes a shell command synchronously with an output handler, passing arguments individually
	/// so that no manual quoting is required.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <returns>The exit code of the executed process.</returns>
	public static int Execute(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler) =>
		ExecuteAsync(fileName, arguments, outputHandler).Result;

	/// <summary>
	/// Executes a shell command synchronously with an output handler and the given process options,
	/// passing arguments individually so that no manual quoting is required.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <param name="outputHandler">
	/// The handler for processing command output. Not invoked when <see cref="CommandOptions.Elevation"/>
	/// is <see cref="Elevation.Elevated"/> on Windows because elevation requires <c>UseShellExecute</c>,
	/// which is incompatible with output redirection.
	/// </param>
	/// <param name="options">The options shaping the process the command runs in.</param>
	/// <returns>The exit code of the executed process.</returns>
	public static int Execute(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options) =>
		ExecuteAsync(fileName, arguments, outputHandler, options).Result;

	/// <summary>
	/// Executes a shell command asynchronously
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static async Task<int> ExecuteAsync(string command)
		=> await ExecuteAsync(command, new OutputHandler()).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously with an output handler.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static async Task<int> ExecuteAsync(string command, OutputHandler outputHandler)
		=> await ExecuteAsync(command, outputHandler, Elevation.Default).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously at the specified elevation level.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static async Task<int> ExecuteAsync(string command, Elevation elevation)
		=> await ExecuteAsync(command, new(), elevation).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously with an output handler at the specified elevation level.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">
	/// The handler for processing command output. Not invoked when <paramref name="elevation"/> is
	/// <see cref="Elevation.Elevated"/> on Windows because elevation requires <c>UseShellExecute</c>,
	/// which is incompatible with output redirection.
	/// </param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static async Task<int> ExecuteAsync(string command, OutputHandler outputHandler, Elevation elevation)
		=> await ExecuteAsync(command, outputHandler, elevation, CancellationToken.None).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously, cancelling the process if the token is signalled.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="cancellationToken">
	/// A token that, when cancelled, terminates the running process and its children.
	/// </param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static async Task<int> ExecuteAsync(string command, CancellationToken cancellationToken)
		=> await ExecuteAsync(command, new OutputHandler(), cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously with an output handler, cancelling the process if the
	/// token is signalled.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <param name="cancellationToken">
	/// A token that, when cancelled, terminates the running process and its children.
	/// </param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static async Task<int> ExecuteAsync(string command, OutputHandler outputHandler, CancellationToken cancellationToken)
		=> await ExecuteAsync(command, outputHandler, Elevation.Default, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously with an output handler at the specified elevation
	/// level, cancelling the process if the token is signalled.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">
	/// The handler for processing command output. Not invoked when <paramref name="elevation"/> is
	/// <see cref="Elevation.Elevated"/> on Windows because elevation requires <c>UseShellExecute</c>,
	/// which is incompatible with output redirection.
	/// </param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <param name="cancellationToken">
	/// A token that, when cancelled, terminates the running process and its children.
	/// </param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	/// <exception cref="OperationCanceledException">The token was cancelled before the process exited.</exception>
	[Obsolete("A command string is split on its first space, which cannot handle an executable path "
		+ "containing spaces. Use the overload taking a file name and an argument list instead.")]
	public static async Task<int> ExecuteAsync(string command, OutputHandler outputHandler, Elevation elevation, CancellationToken cancellationToken)
	{
		Ensure.NotNull(command);
		Ensure.NotNull(outputHandler);

		string[] commandParts = command.Split(' ', 2);

		string filename = commandParts[0];
		string arguments = commandParts.Length > 1 ? commandParts[1] : string.Empty;

		ProcessStartInfo startInfo = CreateStartInfo(filename, outputHandler, new CommandOptions { Elevation = elevation }, out bool useElevation);
		startInfo.Arguments = arguments;

		return await RunAsync(startInfo, outputHandler, useElevation, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Executes a command asynchronously, passing arguments individually so that no manual quoting
	/// is required.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string fileName, IEnumerable<string> arguments)
		=> await ExecuteAsync(fileName, arguments, new OutputHandler()).ConfigureAwait(false);

	/// <summary>
	/// Executes a command asynchronously with an output handler, passing arguments individually so
	/// that no manual quoting is required.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler)
		=> await ExecuteAsync(fileName, arguments, outputHandler, CancellationToken.None).ConfigureAwait(false);

	/// <summary>
	/// Executes a command asynchronously with an output handler, passing arguments individually so
	/// that no manual quoting is required, and cancelling the process if the token is signalled.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <param name="cancellationToken">
	/// A token that, when cancelled, terminates the running process and its children.
	/// </param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CancellationToken cancellationToken)
		=> await ExecuteAsync(fileName, arguments, outputHandler, Elevation.Default, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Executes a command asynchronously with an output handler at the specified elevation level,
	/// passing arguments individually so that no manual quoting is required, and cancelling the
	/// process if the token is signalled.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <param name="outputHandler">
	/// The handler for processing command output. Not invoked when <paramref name="elevation"/> is
	/// <see cref="Elevation.Elevated"/> on Windows because elevation requires <c>UseShellExecute</c>,
	/// which is incompatible with output redirection.
	/// </param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <param name="cancellationToken">
	/// A token that, when cancelled, terminates the running process and its children.
	/// </param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	/// <exception cref="OperationCanceledException">The token was cancelled before the process exited.</exception>
	public static async Task<int> ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, Elevation elevation, CancellationToken cancellationToken)
		=> await ExecuteAsync(fileName, arguments, outputHandler, new CommandOptions { Elevation = elevation }, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Executes a command asynchronously with an output handler and the given process options,
	/// passing arguments individually so that no manual quoting is required.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <param name="outputHandler">
	/// The handler for processing command output. Not invoked when <see cref="CommandOptions.Elevation"/>
	/// is <see cref="Elevation.Elevated"/> on Windows because elevation requires <c>UseShellExecute</c>,
	/// which is incompatible with output redirection.
	/// </param>
	/// <param name="options">The options shaping the process the command runs in.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options)
		=> await ExecuteAsync(fileName, arguments, outputHandler, options, CancellationToken.None).ConfigureAwait(false);

	/// <summary>
	/// Executes a command asynchronously with an output handler and the given process options,
	/// passing arguments individually so that no manual quoting is required, and cancelling the
	/// process if the token is signalled.
	/// </summary>
	/// <param name="fileName">The executable to run.</param>
	/// <param name="arguments">The arguments to pass, each as a separate unquoted value.</param>
	/// <param name="outputHandler">
	/// The handler for processing command output. Not invoked when <see cref="CommandOptions.Elevation"/>
	/// is <see cref="Elevation.Elevated"/> on Windows because elevation requires <c>UseShellExecute</c>,
	/// which is incompatible with output redirection.
	/// </param>
	/// <param name="options">The options shaping the process the command runs in.</param>
	/// <param name="cancellationToken">
	/// A token that, when cancelled, terminates the running process and its children.
	/// </param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	/// <exception cref="OperationCanceledException">The token was cancelled before the process exited.</exception>
	public static async Task<int> ExecuteAsync(string fileName, IEnumerable<string> arguments, OutputHandler outputHandler, CommandOptions options, CancellationToken cancellationToken)
	{
		Ensure.NotNull(fileName);
		Ensure.NotNull(arguments);
		Ensure.NotNull(outputHandler);
		Ensure.NotNull(options);

		ProcessStartInfo startInfo = CreateStartInfo(fileName, outputHandler, options, out bool useElevation);
		SetArguments(startInfo, arguments);

		return await RunAsync(startInfo, outputHandler, useElevation, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Resolves a relative executable path that names a directory, such as <c>./build.sh</c> or
	/// <c>tools/build.sh</c>, against the working directory the command runs in.
	/// </summary>
	/// <param name="fileName">The executable as the caller gave it.</param>
	/// <param name="workingDirectory">The command's working directory, or <see langword="null"/> if none was set.</param>
	/// <param name="isWindows">Whether a backslash also separates directories.</param>
	/// <returns>The path to start.</returns>
	/// <remarks>
	/// Process.Start resolves a relative path against the calling process's current directory, not
	/// against <see cref="ProcessStartInfo.WorkingDirectory"/>, so without this <c>./build.sh</c> ran
	/// the caller's copy, or failed to start, whatever <see cref="CommandOptions.WorkingDirectory"/>
	/// said. Resolving it here does what <c>cd dir &amp;&amp; ./build.sh</c> does in a shell. A bare name
	/// such as <c>git</c> has no directory part and keeps the usual search, and an absolute path is
	/// already unambiguous.
	/// </remarks>
	private static string ResolveFileName(string fileName, string? workingDirectory, bool isWindows)
	{
		if (workingDirectory is null || Path.IsPathRooted(fileName))
		{
			return fileName;
		}

		bool hasDirectoryPart = fileName.Contains('/') || (isWindows && fileName.Contains('\\'));
		return hasDirectoryPart ? Path.GetFullPath(Path.Combine(workingDirectory, fileName)) : fileName;
	}

	private static ProcessStartInfo CreateStartInfo(string fileName, OutputHandler outputHandler, CommandOptions options, out bool useElevation)
	{
		bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		useElevation = options.Elevation == Elevation.Elevated && isWindows;

		if (useElevation && options.StandardInput == StandardInputMode.Closed)
		{
			// Same reason as the environment check below: UseShellExecute offers no stream to
			// redirect, so the request cannot be honoured. Saying so beats starting a command whose
			// standard input is still the caller's, which is the hang StandardInputMode.Closed exists
			// to prevent.
			throw new ArgumentException(
				"Standard input cannot be closed for an elevated command, because elevation requires UseShellExecute.",
				nameof(options));
		}

		if (useElevation && options.EnvironmentVariables is not null)
		{
			// Elevation needs UseShellExecute, which starts the process through the shell and offers
			// nowhere to put an environment. Saying so here beats letting Process.Start fail with a
			// message that does not mention either setting, and beats silently dropping variables a
			// caller may be relying on for credentials or machine-parseable output.
			throw new ArgumentException(
				"Environment variables cannot be set for an elevated command, because elevation requires UseShellExecute.",
				nameof(options));
		}

		ProcessStartInfo startInfo = new()
		{
			FileName = ResolveFileName(fileName, options.WorkingDirectory?.WeakString, isWindows),
			CreateNoWindow = true,
		};

		if (options.WorkingDirectory is not null)
		{
			startInfo.WorkingDirectory = options.WorkingDirectory.WeakString;
		}

		if (useElevation)
		{
			startInfo.UseShellExecute = true;
			startInfo.Verb = "runas";
		}
		else
		{
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.StandardOutputEncoding = outputHandler.Encoding;
			startInfo.StandardErrorEncoding = outputHandler.Encoding;
			startInfo.RedirectStandardInput = options.StandardInput == StandardInputMode.Closed;
			startInfo.UseShellExecute = false;

			if (options.EnvironmentVariables is not null)
			{
				foreach (KeyValuePair<string, string?> variable in options.EnvironmentVariables)
				{
					if (variable.Value is null)
					{
						_ = startInfo.Environment.Remove(variable.Key);
					}
					else
					{
						startInfo.Environment[variable.Key] = variable.Value;
					}
				}

				string? overlayPath = FindOverlayPath(options.EnvironmentVariables, isWindows);
				if (overlayPath is not null)
				{
					startInfo.FileName = ResolveOnSearchPath(startInfo.FileName, overlayPath, startInfo.Environment, isWindows) ?? startInfo.FileName;
				}
			}

			if (isWindows)
			{
				startInfo.LoadUserProfile = true;
			}
		}

		return startInfo;
	}

	/// <summary>
	/// Returns the <c>PATH</c> value an environment overlay sets, if it sets one.
	/// </summary>
	/// <param name="environmentVariables">The overlay the caller passed.</param>
	/// <param name="isWindows">Whether variable names compare case-insensitively.</param>
	/// <returns>
	/// The overlay's <c>PATH</c>, or <see langword="null"/> when the overlay leaves <c>PATH</c> alone or removes it.
	/// A removed <c>PATH</c> gives no directories to search, so the executable is found the way it is without an
	/// overlay.
	/// </returns>
	internal static string? FindOverlayPath(IReadOnlyDictionary<string, string?> environmentVariables, bool isWindows)
	{
		StringComparison comparison = isWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		string? path = null;
		foreach (KeyValuePair<string, string?> variable in environmentVariables)
		{
			if (string.Equals(variable.Key, "PATH", comparison))
			{
				path = variable.Value;
			}
		}

		return path;
	}

	/// <summary>
	/// Looks a bare executable name up on the <c>PATH</c> an environment overlay gives the command.
	/// </summary>
	/// <param name="fileName">The executable as the caller gave it.</param>
	/// <param name="searchPath">The overlay's <c>PATH</c>.</param>
	/// <param name="environment">The command's environment, which supplies <c>PATHEXT</c> on Windows.</param>
	/// <param name="isWindows">Whether to try the <c>PATHEXT</c> extensions and accept a backslash as a separator.</param>
	/// <returns>The full path of the first match, or <see langword="null"/> to keep the usual lookup.</returns>
	/// <remarks>
	/// With <see cref="ProcessStartInfo.UseShellExecute"/> off, <c>Process.Start</c> searches the calling process's
	/// <c>PATH</c>, not the one in <see cref="ProcessStartInfo.Environment"/>. Without this, an overlay that points
	/// <c>PATH</c> at a pinned toolchain either failed to start a tool only it holds or started a same-named tool from
	/// the caller's <c>PATH</c>. A name with a directory part is a path rather than a name to search for, so it is
	/// left alone, and so are relative <c>PATH</c> entries, which would otherwise depend on the caller's directory.
	/// </remarks>
	internal static string? ResolveOnSearchPath(string fileName, string searchPath, IDictionary<string, string?> environment, bool isWindows)
	{
		bool hasDirectoryPart = fileName.Contains('/') || (isWindows && (fileName.Contains('\\') || fileName.Contains(':')));
		if (fileName.Length == 0 || hasDirectoryPart)
		{
			return null;
		}

		string[] candidates = [fileName];
		if (isWindows && !Path.HasExtension(fileName))
		{
			_ = environment.TryGetValue("PATHEXT", out string? pathExt);
			string[] extensions = (pathExt ?? ".COM;.EXE;.BAT;.CMD").Split([';'], StringSplitOptions.RemoveEmptyEntries);
			candidates = Array.ConvertAll(extensions, extension => fileName + extension.Trim());
		}

		foreach (string directory in searchPath.Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries))
		{
			foreach (string candidate in candidates)
			{
				string? match = TryExecutable(directory.Trim().Trim('"'), candidate);
				if (match is not null)
				{
					return match;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Returns the full path of <paramref name="fileName"/> in <paramref name="directory"/> if it is a file the
	/// command could start.
	/// </summary>
	/// <param name="directory">A directory from the search path.</param>
	/// <param name="fileName">The file name to look for.</param>
	/// <returns>
	/// The full path, or <see langword="null"/> if there is no such file. Outside Windows the calling process also
	/// needs permission to execute the file, as <c>access(X_OK)</c> reports it, on every target framework.
	/// </returns>
	private static string? TryExecutable(string directory, string fileName)
	{
		try
		{
			// A rooted name would make Path.Combine drop the directory. The caller only passes bare names, so this
			// is a guard rather than a case that arises.
			if (!Path.IsPathRooted(directory) || Path.IsPathRooted(fileName))
			{
				return null;
			}

			string candidate = Path.Combine(directory, fileName);
			if (!File.Exists(candidate))
			{
				return null;
			}

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !NativeMethods.CanExecute(candidate))
			{
				return null;
			}

			return candidate;
		}
		catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
		{
			// An entry that is not a usable path is skipped, the way a shell skips it.
			return null;
		}
	}

#if NETSTANDARD2_0
	// ProcessStartInfo.ArgumentList is not part of the netstandard2.0 surface, so build an escaped
	// command line by hand on that target only.
	private static void SetArguments(ProcessStartInfo startInfo, IEnumerable<string> arguments) =>
		startInfo.Arguments = string.Join(" ", arguments.Select(EscapeArgument));
#else
	private static void SetArguments(ProcessStartInfo startInfo, IEnumerable<string> arguments)
	{
		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}
	}
#endif

#if NETSTANDARD2_0
	/// <summary>
	/// Quotes a single argument following the rules that <c>CommandLineToArgvW</c> applies when it
	/// splits a command line back into arguments.
	/// </summary>
	private static string EscapeArgument(string argument)
	{
		Ensure.NotNull(argument);

		if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '\n', '\v', '"']) < 0)
		{
			return argument;
		}

		StringBuilder builder = new();
		_ = builder.Append('"');

		for (int i = 0; i < argument.Length; i++)
		{
			int backslashes = 0;
			while (i < argument.Length && argument[i] == '\\')
			{
				backslashes++;
				i++;
			}

			if (i == argument.Length)
			{
				// Trailing backslashes must be doubled so they do not escape the closing quote.
				_ = builder.Append('\\', backslashes * 2);
				break;
			}

			if (argument[i] == '"')
			{
				// Backslashes preceding a quote are doubled, and the quote itself is escaped.
				_ = builder.Append('\\', (backslashes * 2) + 1).Append('"');
			}
			else
			{
				_ = builder.Append('\\', backslashes).Append(argument[i]);
			}
		}

		_ = builder.Append('"');
		return builder.ToString();
	}
#endif

	private static async Task<int> RunAsync(ProcessStartInfo startInfo, OutputHandler outputHandler, bool useElevation, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// A handler can be reused, including by runs that overlap, so each run feeds a handler of its
		// own: a partial line one run leaves behind, whether it was cancelled, failed or is still going,
		// is never glued onto another run's output. Reset covers a stateful handler that returns itself.
		outputHandler = outputHandler.CreateRunHandler();
		outputHandler.Reset();

		using Process process = new() { StartInfo = startInfo };

		process.Start();

		if (startInfo.RedirectStandardInput)
		{
			// Redirecting alone would leave the command holding a pipe nobody writes to, which is the
			// same wait as inheriting. Closing it is what turns a read into end of stream.
			process.StandardInput.Close();
		}

		// Killing the process is what makes the await actually stop: without it a cancelled wait
		// would leave the child running unsupervised.
		using CancellationTokenRegistration registration = cancellationToken.Register(() => TryKill(process));

		try
		{
			if (useElevation)
			{
				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
				using AsyncProcessStreamReader outputReader = new(process, outputHandler);

				try
				{
					await outputReader.Start(cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					// A failed read has stopped draining a pipe, so the command blocks once it fills it
					// and would never exit on its own. Kill it so the failure can be reported rather
					// than waited on, and so the command is not left behind blocked on the write.
					TryKill(process);
					throw;
				}

				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException)
		{
			// The registration alone does not guarantee the kill. A token runs its callbacks newest
			// first, and the wait registered its own after this one, so the wait can end this call
			// and dispose the registration before the kill callback has run. That leaves the whole
			// tree running unsupervised. Killing here as well makes it certain; a second kill of a
			// process that has already gone is a no-op.
			TryKill(process);
			throw;
		}

		// Cancellation reaches the wait two ways at once: the registration above kills the process,
		// and the wait separately observes the token. The kill makes the process exit fast enough
		// that the normal-exit path can win, which would return the killed process's exit code and
		// throw nothing. Re-checking here makes both paths end the same way, so a cancelled call is
		// never mistaken for a command that genuinely failed.
		cancellationToken.ThrowIfCancellationRequested();

		if (!useElevation)
		{
			// Both streams have been read to their end, so anything a handler is still holding back
			// waiting for a line break will never get one.
			outputHandler.Complete();
		}

		return process.ExitCode;
	}

	private static void TryKill(Process process) => TryKill(process, Kill);

	/// <summary>
	/// Kills <paramref name="process"/> through <paramref name="kill"/>, treating every way the kill
	/// can fail as best effort.
	/// </summary>
	/// <remarks>
	/// This runs inside the cancellation token's registration, so anything it throws comes out of
	/// the caller's <see cref="CancellationTokenSource.Cancel()"/>, and it runs in catch blocks,
	/// where anything it throws replaces the exception being reported. It must never throw.
	/// </remarks>
	/// <param name="process">The process to kill.</param>
	/// <param name="kill">Performs the kill. A parameter so tests can make it fail.</param>
	internal static void TryKill(Process process, Action<Process> kill)
	{
		try
		{
			if (!process.HasExited)
			{
				kill(process);
			}
		}
		catch (InvalidOperationException)
		{
			// The process exited between the HasExited check and the kill.
		}
		catch (System.ComponentModel.Win32Exception)
		{
			// The process could not be terminated, typically because it is already exiting.
		}
		catch (NotSupportedException)
		{
			// Terminating a remote process is not supported.
		}
		catch (AggregateException)
		{
			// Kill(entireProcessTree) reports descendants it could not terminate, such as ones owned
			// by another user or running elevated, this way. It carries on past each failure, so
			// everything it could kill has already been killed.
		}
	}

#if NETSTANDARD2_0 || NETSTANDARD2_1
	// Process.Kill(bool) requires .NET Core 3.0 or later, so the older targets can only terminate
	// the process itself and not any grandchildren it spawned.
	private static void Kill(Process process) => process.Kill();
#else
	private static void Kill(Process process) => process.Kill(entireProcessTree: true);
#endif
}
