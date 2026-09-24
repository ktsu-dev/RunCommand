// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using System.Diagnostics;

internal sealed class AsyncProcessStreamReader(Process process, OutputHandler outputHandler) : IDisposable
{
	private const char ByteOrderMark = '\uFEFF';

	private readonly char[] outputBuffer = new char[4096];
	private readonly char[] errorBuffer = new char[4096];

	private bool outputHasEmitted;
	private bool errorHasEmitted;

	// Read the raw pipes with the caller's encoding rather than through
	// process.StandardOutput/StandardError. Process builds those StreamReaders with byte-order-mark
	// detection switched on, which replaces the encoding the caller asked for whenever the output
	// happens to begin with a BOM. Output starting with FF FE was decoded as UTF-16LE no matter what
	// OutputHandler.Encoding said, and a strict encoding reported no error on bytes it should have
	// rejected.
	private readonly StreamReader outputStream =
		new(process.StandardOutput.BaseStream, outputHandler.Encoding, detectEncodingFromByteOrderMarks: false);

	private readonly StreamReader errorStream =
		new(process.StandardError.BaseStream, outputHandler.Encoding, detectEncodingFromByteOrderMarks: false);

	public void Dispose()
	{
		outputStream.Dispose();
		errorStream.Dispose();
	}

	internal async Task Start()
	{
		Task outputTask = Task.CompletedTask;
		Task errorTask = Task.CompletedTask;

		// Continuously read until the process has exited.
		do
		{
			// A faulted task is a completed one, so without this the checks below would replace a
			// failed read with a fresh one and the failure it carries would never be observed. That
			// made a decode error on a long-running command a coin toss: it surfaced only when the
			// process happened to exit before the loop came back around.
			if (outputTask.IsFaulted || errorTask.IsFaulted)
			{
				break;
			}

			if (outputTask.IsCompleted)
			{
				outputTask = ReadAndCallback(outputStream, outputBuffer, outputHandler.HandleStandardOutputData, isStandardOutput: true);
			}

			if (errorTask.IsCompleted)
			{
				errorTask = ReadAndCallback(errorStream, errorBuffer, outputHandler.HandleStandardErrorData, isStandardOutput: false);
			}

			await Task.WhenAny(outputTask, errorTask).ConfigureAwait(false);

		} while (!process.HasExited);

		await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

		// Read any remaining data after process exit.
		outputTask = ReadAndCallback(outputStream, outputBuffer, outputHandler.HandleStandardOutputData, isStandardOutput: true);
		errorTask = ReadAndCallback(errorStream, errorBuffer, outputHandler.HandleStandardErrorData, isStandardOutput: false);
		await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
	}

	private async Task ReadAndCallback(StreamReader streamReader, char[] buffer, Action<string>? onData, bool isStandardOutput) =>
		await streamReader.ReadAsync(buffer, 0, buffer.Length)
			.ContinueWith(t => ReadCallback(t, buffer, onData, isStandardOutput), TaskScheduler.Current)
			.ConfigureAwait(false);

	private void ReadCallback(Task<int> readTask, char[] buffer, Action<string>? onData, bool isStandardOutput)
	{
		int charsRead = readTask.Result;

		if (charsRead <= 0)
		{
			return;
		}

		string data = new(buffer, 0, charsRead);
		data = StripLeadingByteOrderMark(data, isStandardOutput);

		if (data.Length > 0)
		{
			onData?.Invoke(data);
		}
	}

	/// <summary>
	/// Drops a byte order mark from the front of a stream's first chunk.
	/// </summary>
	/// <remarks>
	/// Turning off the detection above also turned off the stripping that came with it, and a
	/// leading U+FEFF in captured output is a change no caller asked for. Every encoding decodes its
	/// own BOM to U+FEFF, so dropping that one character covers each of them without guessing at the
	/// encoding. A BOM belonging to a different encoding no longer decodes to U+FEFF, which is
	/// exactly the case that should surface as mis-decoded bytes rather than be silently honoured.
	/// </remarks>
	/// <param name="data">The chunk just read.</param>
	/// <param name="isStandardOutput">Whether the chunk came from standard output.</param>
	/// <returns>The chunk, less a leading byte order mark if this was the stream's first.</returns>
	private string StripLeadingByteOrderMark(string data, bool isStandardOutput)
	{
		bool hasEmitted = isStandardOutput ? outputHasEmitted : errorHasEmitted;

		if (isStandardOutput)
		{
			outputHasEmitted = true;
		}
		else
		{
			errorHasEmitted = true;
		}

		if (hasEmitted || data[0] != ByteOrderMark)
		{
			return data;
		}

		return data[1..];
	}
}
