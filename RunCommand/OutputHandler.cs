// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RunCommand;

using System.Text;

/// <summary>
/// Handles the output from a command line process, processing both standard output and standard error streams.
/// It processes the data in undelimited chunks, calling the specified actions for each chunk received.
/// </summary>
public class OutputHandler
{
	/// <summary>
	/// Gets or sets the encoding used for the output data.
	/// </summary>
	public Encoding Encoding { get; init; }

	/// <summary>
	/// Action to handle standard output data.
	/// </summary>
	protected Action<string>? OnStandardOutput { get; init; }

	/// <summary>
	/// Action to handle standard error data.
	/// </summary>
	protected Action<string>? OnStandardError { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OutputHandler"/> class.
	/// </summary>
	/// <param name="onStandardOutput">The action to handle standard output data.</param>
	/// <param name="onStandardError">The action to handle standard error data.</param>
	/// <param name="encoding">The encoding used for the output data. Defaults to UTF-8 if not specified.</param>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "It's weird when we inherit from this class")]
	public OutputHandler(Action<string>? onStandardOutput = null, Action<string>? onStandardError = null, Encoding? encoding = null)
	{
		OnStandardOutput = onStandardOutput;
		OnStandardError = onStandardError;
		Encoding = encoding ?? Encoding.UTF8;
	}

	/// <summary>
	/// Handles the data received from the standard output stream.
	/// </summary>
	/// <param name="data">The data received from the standard output stream.</param>
	/// <exception cref="ArgumentNullException">Thrown when the data is null.</exception>
	internal virtual void HandleStandardOutputData(string data)
	{
		ArgumentNullException.ThrowIfNull(data);
		OnStandardOutput?.Invoke(data);
	}

	/// <summary>
	/// Handles the data received from the standard error stream.
	/// </summary>
	/// <param name="data">The data received from the standard error stream.</param>
	/// <exception cref="ArgumentNullException">Thrown when the data is null.</exception>
	internal virtual void HandleStandardErrorData(string data)
	{
		ArgumentNullException.ThrowIfNull(data);
		OnStandardError?.Invoke(data);
	}
}
