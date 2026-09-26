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
	internal string outputBuffer = "";

	/// <summary>
	/// Buffer to store incomplete lines from standard error.
	/// </summary>
	internal string errorBuffer = "";

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
		ProcessDataByLine(data, ref outputBuffer, OnStandardOutput);
	}

	/// <summary>
	/// Handles the data received from the standard error stream.
	/// </summary>
	/// <param name="data">The data received from the standard error stream.</param>
	/// <exception cref="ArgumentNullException">Thrown when the data is null.</exception>
	internal override void HandleStandardErrorData(string data)
	{
		Ensure.NotNull(data);
		ProcessDataByLine(data, ref errorBuffer, OnStandardError);
	}

	/// <summary>
	/// Processes the data by line, invoking the specified action for each line received.
	/// </summary>
	/// <param name="data">The data to be processed.</param>
	/// <param name="buffer">The buffer to store incomplete lines.</param>
	/// <param name="onLineReceived">The action to be invoked for each complete line received.</param>
	/// <remarks>
	/// Line endings are recognised on the buffered text rather than on each chunk, so a CRLF split across two
	/// reads is still one line break. A trailing CR stays in the buffer until the next chunk shows whether an LF follows.
	/// </remarks>
	private static void ProcessDataByLine(string data, ref string buffer, Action<string>? onLineReceived)
	{
		buffer += data;
		int lineStart = 0;
		int i = 0;
		while (i < buffer.Length)
		{
			char c = buffer[i];
			if (c == '\r')
			{
				if (i == buffer.Length - 1)
				{
					break;
				}

				onLineReceived?.Invoke(buffer[lineStart..i]);
				i += buffer[i + 1] == '\n' ? 2 : 1;
				lineStart = i;
			}
			else if (IsLineBreak(c))
			{
				onLineReceived?.Invoke(buffer[lineStart..i]);
				i++;
				lineStart = i;
			}
			else
			{
				i++;
			}
		}

		buffer = buffer[lineStart..];
	}

	/// <summary>
	/// Determines whether a character other than CR ends a line, matching the set <c>string.ReplaceLineEndings</c> recognises.
	/// </summary>
	/// <param name="c">The character to test.</param>
	/// <returns><see langword="true"/> if <paramref name="c"/> is LF, NEL, LS, PS or FF; otherwise <see langword="false"/>.</returns>
	private static bool IsLineBreak(char c) => c is '\n' or '\u0085' or '\u2028' or '\u2029' or '\f';
}
