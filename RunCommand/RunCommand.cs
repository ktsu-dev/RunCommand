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
	public static void Execute(string command) =>
		ExecuteAsync(command).Wait();

	/// <summary>
	/// Executes a shell command synchronously with an output handler.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	public static void Execute(string command, OutputHandler outputHandler) =>
		ExecuteAsync(command, outputHandler).Wait();

	/// <summary>
	/// Executes a shell command asynchronously.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task ExecuteAsync(string command)
		=> await ExecuteAsync(command, new()).ConfigureAwait(false);

	/// <summary>
	/// Executes a shell command asynchronously with an output handler.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="outputHandler">The handler for processing command output.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task ExecuteAsync(string command, OutputHandler outputHandler)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentNullException.ThrowIfNull(outputHandler);

		var commandParts = command.Split(' ', 2);

		var filename = commandParts[0];
		var arguments = commandParts.Length > 1 ? commandParts[1] : string.Empty;

		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = filename,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = outputHandler.Encoding,
				StandardErrorEncoding = outputHandler.Encoding,
				UseShellExecute = false,
				CreateNoWindow = true,
			}
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		if (isWindows)
		{
			process.StartInfo.LoadUserProfile = true;
		}

		process.Start();

		AsyncProcessStreamReader outputReader = new(process, outputHandler);

		var outputTask = outputReader.Start();
		var processTask = process.WaitForExitAsync();

		await Task.WhenAll(outputTask, processTask).ConfigureAwait(false);
	}
}
