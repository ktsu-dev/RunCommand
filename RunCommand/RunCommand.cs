// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RunCommand;

using System.Diagnostics;
using System.Runtime.InteropServices;

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
	public static int Execute(string command) =>
		ExecuteAsync(command).Result;

	/// <summary>
	/// Executes a shell command synchronously with an output handler.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <returns>The exit code of the executed process.</returns>
	public static int Execute(string command, OutputHandler outputHandler) =>
		ExecuteAsync(command, outputHandler).Result;

	/// <summary>
	/// Executes a shell command synchronously at the specified elevation level.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <returns>The exit code of the executed process.</returns>
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
	public static int Execute(string command, OutputHandler outputHandler, Elevation elevation) =>
		ExecuteAsync(command, outputHandler, elevation).Result;

	/// <summary>
	/// Executes a shell command asynchronously
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string command)
		=> await ExecuteAsync(command, new()).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously with an output handler.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string command, OutputHandler outputHandler)
		=> await ExecuteAsync(command, outputHandler, Elevation.Default).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously at the specified elevation level.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="elevation">The privilege level under which to run the command.</param>
	/// <returns>A task representing the asynchronous operation with the process exit code.</returns>
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
	public static async Task<int> ExecuteAsync(string command, OutputHandler outputHandler, Elevation elevation)
	{
		Ensure.NotNull(command);
		Ensure.NotNull(outputHandler);

		string[] commandParts = command.Split(' ', 2);

		string filename = commandParts[0];
		string arguments = commandParts.Length > 1 ? commandParts[1] : string.Empty;

		bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		bool useElevation = elevation == Elevation.Elevated && isWindows;

		ProcessStartInfo startInfo = new()
		{
			FileName = filename,
			Arguments = arguments,
			CreateNoWindow = true,
		};

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

			if (isWindows)
			{
				startInfo.LoadUserProfile = true;
			}
		}

		using Process process = new() { StartInfo = startInfo };

		process.Start();

		if (useElevation)
		{
			await process.WaitForExitAsync().ConfigureAwait(false);
		}
		else
		{
			AsyncProcessStreamReader outputReader = new(process, outputHandler);
			Task outputTask = outputReader.Start();
			Task processTask = process.WaitForExitAsync();
			await Task.WhenAll(outputTask, processTask).ConfigureAwait(false);
		}

		return process.ExitCode;
	}
}
