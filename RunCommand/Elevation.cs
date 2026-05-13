// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RunCommand;

/// <summary>
/// Specifies the privilege level under which a command should be executed.
/// </summary>
public enum Elevation
{
	/// <summary>
	/// Run the command with the current process's privileges. Standard output and standard error are captured.
	/// </summary>
	Default,

	/// <summary>
	/// Run the command with elevated privileges. On Windows, launches the process with the
	/// <c>runas</c> verb, prompting the user for UAC consent; output cannot be captured in this mode
	/// because elevation requires <c>UseShellExecute</c>. On non-Windows platforms this value has no
	/// effect; prefix the command with <c>sudo</c> there instead.
	/// </summary>
	Elevated,
}
