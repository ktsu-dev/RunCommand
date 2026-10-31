// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using System.Text;

/// <summary>
/// Handles the output from a command process, processing both standard output and standard error streams.
/// It processes the data line-by-line, calling the specified actions for each line received.
/// </summary>
public class LineOutputHandler : OutputHandler
{
	/// <summary>
	/// Buffer to store incomplete lines from standard output.
	/// </summary>
	/// <remarks>
	/// Replaced rather than cleared by <see cref="Reset"/>, so a delivery still in progress from a
	/// cancelled run finishes into the old buffer instead of the new run's.
	/// </remarks>
	internal LineBuffer outputBuffer = new();

	/// <summary>
	/// Buffer to store incomplete lines from standard error.
	/// </summary>
	/// <remarks>See <see cref="outputBuffer"/>.</remarks>
	internal LineBuffer errorBuffer = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="LineOutputHandler"/> class.
	/// </summary>
	/// <param name="onStandardOutput">The action to handle standard output data.</param>
	/// <param name="onStandardError">The action to handle standard error data.</param>
	/// <param name="encoding">The encoding used for the output data. Defaults to UTF-8 if not specified.</param>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "It's weird when we inherit from this class")]
	public LineOutputHandler(Action<string>? onStandardOutput = null, Action<string>? onStandardError = null, Encoding? encoding = null)
		: base(onStandardOutput, onStandardError, encoding) { }

	/// <summary>
	/// Handles the data received from the standard output stream.
	/// </summary>
	/// <param name="data">The data received from the standard output stream.</param>
	/// <exception cref="ArgumentNullException">Thrown when the data is null.</exception>
	internal override void HandleStandardOutputData(string data)
	{
		Ensure.NotNull(data);
		ProcessDataByLine(data, outputBuffer, OnStandardOutput);
	}

	/// <summary>
	/// Handles the data received from the standard error stream.
	/// </summary>
	/// <param name="data">The data received from the standard error stream.</param>
	/// <exception cref="ArgumentNullException">Thrown when the data is null.</exception>
	internal override void HandleStandardErrorData(string data)
	{
		Ensure.NotNull(data);
		ProcessDataByLine(data, errorBuffer, OnStandardError);
	}

	/// <summary>
	/// Delivers the text left in each buffer as a final line, since no line break will now arrive to end it,
	/// and leaves both buffers empty so a later run on this handler starts clean.
	/// </summary>
	internal override void Complete()
	{
		FlushBuffer(outputBuffer, OnStandardOutput);
		FlushBuffer(errorBuffer, OnStandardError);
	}

	/// <summary>
	/// Discards any partial line, and any LF still due to be skipped after a CR, that a cancelled or
	/// failed run left in the buffers.
	/// </summary>
	internal override void Reset()
	{
		outputBuffer = new();
		errorBuffer = new();
	}

	/// <summary>
	/// Invokes <paramref name="onLineReceived"/> with the buffered final line, if there is one, and clears the buffer.
	/// </summary>
	/// <param name="buffer">The buffer holding an incomplete line.</param>
	/// <param name="onLineReceived">The action to be invoked for the final line.</param>
	private static void FlushBuffer(LineBuffer buffer, Action<string>? onLineReceived)
	{
		buffer.SkipLeadingLineFeed = false;

		if (buffer.Text.Length == 0)
		{
			return;
		}

		string line = buffer.Text.ToString();
		buffer.Text.Clear();
		onLineReceived?.Invoke(line);
	}

	/// <summary>
	/// Processes the data by line, invoking the specified action for each line received.
	/// </summary>
	/// <param name="data">The data to be processed.</param>
	/// <param name="buffer">The buffer to store incomplete lines.</param>
	/// <param name="onLineReceived">The action to be invoked for each complete line received.</param>
	/// <remarks>
	/// Line endings are recognised across reads, so a CRLF split across two reads is still one line break.
	/// A CR ends its line at once, even as the last character of a read, so a progress or status line
	/// ending in a bare CR is delivered without waiting for the command's next write. If the next read
	/// then starts with an LF, that LF completes the CRLF and is skipped rather than ending an empty
	/// line. This is what <c>Process.OutputDataReceived</c> does.
	/// Only the newly arrived <paramref name="data"/> is scanned, and the partial line is appended to rather than
	/// copied, so the cost is linear in the output size however long a line grows.
	/// </remarks>
	private static void ProcessDataByLine(string data, LineBuffer buffer, Action<string>? onLineReceived)
	{
		if (data.Length == 0)
		{
			return;
		}

		int i = 0;
		if (buffer.SkipLeadingLineFeed)
		{
			buffer.SkipLeadingLineFeed = false;
			if (data[0] == '\n')
			{
				i = 1;
			}
		}

		int lineStart = i;
		while (i < data.Length)
		{
			char c = data[i];
			if (c == '\r')
			{
				EmitLine(buffer.Text, data, lineStart, i, onLineReceived);
				if (i == data.Length - 1)
				{
					// The LF of a CRLF may arrive with the next read
					buffer.SkipLeadingLineFeed = true;
					return;
				}

				i += data[i + 1] == '\n' ? 2 : 1;
				lineStart = i;
			}
			else if (IsLineBreak(c))
			{
				EmitLine(buffer.Text, data, lineStart, i, onLineReceived);
				i++;
				lineStart = i;
			}
			else
			{
				i++;
			}
		}

		buffer.Text.Append(data, lineStart, data.Length - lineStart);
	}

	/// <summary>
	/// Invokes <paramref name="onLineReceived"/> with the buffered partial line followed by
	/// <paramref name="data"/> from <paramref name="start"/> up to <paramref name="end"/>, and clears the buffer.
	/// </summary>
	/// <param name="buffer">The buffer holding the start of the line from earlier reads.</param>
	/// <param name="data">The data the rest of the line comes from.</param>
	/// <param name="start">The index in <paramref name="data"/> where the rest of the line starts.</param>
	/// <param name="end">The index in <paramref name="data"/> of the line break that ends the line.</param>
	/// <param name="onLineReceived">The action to be invoked for the line.</param>
	private static void EmitLine(StringBuilder buffer, string data, int start, int end, Action<string>? onLineReceived)
	{
		string line;
		if (buffer.Length == 0)
		{
			line = data[start..end];
		}
		else
		{
			buffer.Append(data, start, end - start);
			line = buffer.ToString();
			buffer.Clear();
		}

		onLineReceived?.Invoke(line);
	}

	/// <summary>
	/// Determines whether a character other than CR ends a line, matching the set <c>string.ReplaceLineEndings</c> recognises.
	/// </summary>
	/// <param name="c">The character to test.</param>
	/// <returns><see langword="true"/> if <paramref name="c"/> is LF, NEL, LS, PS or FF; otherwise <see langword="false"/>.</returns>
	private static bool IsLineBreak(char c) => c is '\n' or '\u0085' or '\u2028' or '\u2029' or '\f';

	/// <summary>
	/// The partial line one stream has delivered so far, and whether its last read ended on a CR.
	/// </summary>
	internal sealed class LineBuffer
	{
		/// <summary>
		/// Gets the start of a line whose line break has not arrived yet.
		/// </summary>
		internal StringBuilder Text { get; } = new();

		/// <summary>
		/// Gets or sets a value indicating whether the last read ended on a CR, whose line has already
		/// been delivered, so an LF at the start of the next read completes a CRLF and is skipped.
		/// </summary>
		internal bool SkipLeadingLineFeed { get; set; }

		/// <inheritdoc/>
		public override string ToString() => Text.ToString();
	}
}
