// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
	private static void ProcessDataByLine(string data, ref string buffer, Action<string>? onLineReceived)
	{
		buffer += data.ReplaceLineEndings();
		while (buffer.Contains(Environment.NewLine))
		{
			string[] split = buffer.Split(Environment.NewLine, 2);
			buffer = split.Length == 1
				? string.Empty
				: split[1];

			onLineReceived?.Invoke(split[0]);
		}
	}
}
