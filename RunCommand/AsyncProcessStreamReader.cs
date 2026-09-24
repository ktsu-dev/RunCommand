// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using System.Diagnostics;
using System.Threading;

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

	/// <summary>
	/// Pumps the process's standard output and standard error to the handler until both pipes reach
	/// end of stream, or until <paramref name="cancellationToken"/> is signalled.
	/// </summary>
	/// <remarks>
	/// A read of a redirected pipe returns on new data or on end of stream, and nothing else — there
	/// is no token that reaches into it, and a pipe read is not reliably interruptible even on the
	/// targets whose <see cref="StreamReader"/> offers a cancellable overload. So cancellation is
	/// handled by giving up on the pending read rather than by cancelling it.
	/// <para>
	/// That distinction is the whole point. End of stream means every handle on the write end has
	/// closed, and killing the command closes only the handles the command itself held: one that a
	/// descendant inherited and carried past its parent's death keeps the pipe open. Waiting for end
	/// of stream after a kill therefore waits on something that may never happen, which left a
	/// cancelled call hanging indefinitely.
	/// </para>
	/// </remarks>
	/// <param name="cancellationToken">The token the caller cancelled the run with.</param>
	internal async Task Start(CancellationToken cancellationToken)
	{
		TaskCompletionSource<bool> cancellationSource = new();

		using CancellationTokenRegistration registration = cancellationToken.Register(
			static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
			cancellationSource);

		Task cancelled = cancellationSource.Task;

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

			Task first = await Task.WhenAny(outputTask, errorTask, cancelled).ConfigureAwait(false);

			if (ReferenceEquals(first, cancelled))
			{
				Abandon(outputTask, errorTask);
				return;
			}
		} while (!process.HasExited);

		if (!await DrainOrAbandon(outputTask, errorTask, cancelled).ConfigureAwait(false))
		{
			return;
		}

		// Read any remaining data after process exit.
		outputTask = ReadAndCallback(outputStream, outputBuffer, outputHandler.HandleStandardOutputData, isStandardOutput: true);
		errorTask = ReadAndCallback(errorStream, errorBuffer, outputHandler.HandleStandardErrorData, isStandardOutput: false);
		_ = await DrainOrAbandon(outputTask, errorTask, cancelled).ConfigureAwait(false);
	}

	/// <summary>
	/// Waits for both reads to finish, unless cancellation gets there first.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> when both reads finished, so the caller may carry on;
	/// <see langword="false"/> when cancellation won and the reads were abandoned.
	/// </returns>
	private static async Task<bool> DrainOrAbandon(Task outputTask, Task errorTask, Task cancelled)
	{
		Task reads = Task.WhenAll(outputTask, errorTask);

		if (ReferenceEquals(await Task.WhenAny(reads, cancelled).ConfigureAwait(false), cancelled))
		{
			Abandon(outputTask, errorTask);
			return false;
		}

		// Awaited rather than returned so that a read that failed still throws here, which is what
		// carries a decode error out to the caller.
		await reads.ConfigureAwait(false);
		return true;
	}

	/// <summary>
	/// Leaves reads this call has given up on to end however they end, observing the result.
	/// </summary>
	/// <remarks>
	/// Disposing the readers ends an abandoned read, but it ends by faulting, and a faulted task
	/// nobody ever looks at raises <see cref="TaskScheduler.UnobservedTaskException"/> when it is
	/// finalized. Looking at it here keeps a cancelled run from tripping that on an unrelated thread
	/// later. A read that never ends at all costs a buffer until the process handle is released,
	/// which is the price of not waiting on a pipe the caller has already walked away from.
	/// </remarks>
	private static void Abandon(params Task[] reads)
	{
		foreach (Task read in reads)
		{
			_ = read.ContinueWith(
				static abandoned => _ = abandoned.Exception,
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}
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
