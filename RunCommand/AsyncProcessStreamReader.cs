// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RunCommand;

using System.Diagnostics;

internal class AsyncProcessStreamReader(Process process, OutputHandler outputHandler)
{
	private readonly char[] outputBuffer = new char[4096];
	private readonly char[] errorBuffer = new char[4096];

	internal async Task Start()
	{
		var outputTask = Task.CompletedTask;
		var errorTask = Task.CompletedTask;

		// Continuously read until the process has exited.
		do
		{
			if (outputTask.IsCompleted)
			{
				outputTask = ReadAndCallback(process.StandardOutput, outputBuffer, outputHandler.HandleStandardOutputData);
			}

			if (errorTask.IsCompleted)
			{
				errorTask = ReadAndCallback(process.StandardError, errorBuffer, outputHandler.HandleStandardErrorData);
			}

			await Task.WhenAny(outputTask, errorTask).ConfigureAwait(false);

		} while (!process.HasExited);

		await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

		// Read any remaining data after process exit.
		outputTask = ReadAndCallback(process.StandardOutput, outputBuffer, outputHandler.HandleStandardOutputData);
		errorTask = ReadAndCallback(process.StandardError, errorBuffer, outputHandler.HandleStandardErrorData);
		await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
	}

	private static async Task ReadAndCallback(StreamReader streamReader, char[] buffer, Action<string>? onData) =>
		await streamReader.ReadAsync(buffer, 0, buffer.Length)
			.ContinueWith(t => ReadCallback(t, buffer, onData), TaskScheduler.Current)
			.ConfigureAwait(false);

	private static void ReadCallback(Task<int> readTask, char[] buffer, Action<string>? onData)
	{
		var bytesRead = readTask.Result;

		if (bytesRead > 0)
		{
			string data = new(buffer, 0, bytesRead);
			onData?.Invoke(data);
		}
	}
}
