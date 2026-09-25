// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using System.Collections.Generic;
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
	/// Gets the environment variables to apply over the inherited environment, or
	/// <see langword="null"/> to inherit the calling process's environment unchanged.
	/// </summary>
	/// <remarks>
	/// The entries are an overlay rather than a replacement: a name not listed here keeps whatever
	/// the calling process had. A <see langword="null"/> value removes a variable, matching the
	/// semantics of <see cref="System.Diagnostics.ProcessStartInfo.Environment"/>, which is how a
	/// caller unsets something the parent had set.
	/// </remarks>
	public IReadOnlyDictionary<string, string?>? EnvironmentVariables { get; init; }

	/// <summary>
	/// Gets the privilege level under which to run the command.
	/// </summary>
	public Elevation Elevation { get; init; } = Elevation.Default;

	/// <summary>
	/// Gets what the command's standard input is connected to.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="StandardInputMode.Inherit"/>, which is what commands did before this
	/// option existed. Standard output and standard error are already redirected away from the
	/// caller's console, so a command that prompts cannot be answered anyway; a caller that is not
	/// itself a console generally wants <see cref="StandardInputMode.Closed"/>, so that a command
	/// reading standard input ends rather than waiting on a handle nobody will write to.
	/// </remarks>
	public StandardInputMode StandardInput { get; init; } = StandardInputMode.Inherit;
}
