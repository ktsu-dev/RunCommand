// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using ktsu.Semantics.Paths;

/// <summary>
/// Describes how to shape the process a command runs in, beyond the executable and its arguments.
/// </summary>
/// <remarks>
/// Every member defaults to the behaviour commands had before this type existed, so an instance
/// with nothing set is equivalent to not passing one at all.
/// </remarks>
public sealed record CommandOptions
{
	/// <summary>
	/// Gets the directory the process starts in, or <see langword="null"/> to inherit the current
	/// directory of the calling process.
	/// </summary>
	/// <remarks>
	/// The type is deliberately absolute. A relative directory would have to be resolved against the
	/// calling process's current directory, which is the process-global state this property exists
	/// to stop callers depending on in the first place.
	/// </remarks>
	public AbsoluteDirectoryPath? WorkingDirectory { get; init; }

	/// <summary>
	/// Gets the privilege level under which to run the command.
	/// </summary>
	public Elevation Elevation { get; init; } = Elevation.Default;
}
