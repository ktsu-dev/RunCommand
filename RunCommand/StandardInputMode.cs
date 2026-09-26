// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

/// <summary>
/// Specifies what a command's standard input is connected to.
/// </summary>
public enum StandardInputMode
{
	/// <summary>
	/// Let the command inherit the calling process's standard input.
	/// </summary>
	/// <remarks>
	/// A command that reads standard input then reads the caller's, which is what an interactive
	/// command needs and what commands did before this option existed. It is also a hazard for a
	/// caller that is not a console: if the inherited handle stays open without ever producing data,
	/// a command that reads it blocks until the handle closes, and the run only ends when the caller
	/// cancels it. Prefer <see cref="Closed"/> in a long-running host, where a command that waits
	/// forever is a hang rather than an error.
	/// </remarks>
	Inherit,

	/// <summary>
	/// Redirect the command's standard input and close it immediately, so a read reports end of
	/// stream rather than waiting for input that is never coming.
	/// </summary>
	/// <remarks>
	/// The command is isolated from the caller's standard input, so nothing it reads can consume
	/// input the caller intended for itself.
	/// <para>
	/// This cannot be combined with <see cref="Elevation.Elevated"/> on Windows, because elevation
	/// requires <c>UseShellExecute</c>, which offers no stream to redirect.
	/// </para>
	/// </remarks>
	Closed,
}
