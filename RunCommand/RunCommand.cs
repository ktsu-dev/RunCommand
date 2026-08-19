// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

	private static ProcessStartInfo CreateStartInfo(string fileName, OutputHandler outputHandler, CommandOptions options, out bool useElevation)
	{
		bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		useElevation = options.Elevation == Elevation.Elevated && isWindows;

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
			FileName = fileName,
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
			}

			if (isWindows)
			{
				startInfo.LoadUserProfile = true;
			}
		}

		return startInfo;
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

		using Process process = new() { StartInfo = startInfo };

		process.Start();

		// Killing the process is what makes the await actually stop: without it a cancelled wait
		// would leave the child running unsupervised.
		using CancellationTokenRegistration registration = cancellationToken.Register(() => TryKill(process));

		if (useElevation)
		{
			await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
		}
		else
		{
			AsyncProcessStreamReader outputReader = new(process, outputHandler);
			await Task.WhenAll(outputReader.Start(), process.WaitForExitAsync(cancellationToken)).ConfigureAwait(false);
		}

		// Cancellation reaches the wait two ways at once: the registration above kills the process,
		// and the wait separately observes the token. The kill makes the process exit fast enough
		// that the normal-exit path can win, which would return the killed process's exit code and
		// throw nothing. Re-checking here makes both paths end the same way, so a cancelled call is
		// never mistaken for a command that genuinely failed.
		cancellationToken.ThrowIfCancellationRequested();

		return process.ExitCode;
	}

	private static void TryKill(Process process)
	{
		try
		{
			if (!process.HasExited)
			{
#if NETSTANDARD2_0 || NETSTANDARD2_1
				// Process.Kill(bool) requires .NET Core 3.0 or later, so the older targets can only
				// terminate the process itself and not any grandchildren it spawned.
				process.Kill();
#else
				process.Kill(entireProcessTree: true);
#endif
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
	}
}
