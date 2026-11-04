// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Calls into the C library on Linux and macOS.
/// </summary>
internal static class NativeMethods
{
	/// <summary>The <c>X_OK</c> mode of <c>access</c>: test for permission to execute.</summary>
	private const int ExecuteOk = 1;

	/// <summary>
	/// Returns whether the calling process may execute the file at <paramref name="path"/>.
	/// </summary>
	/// <param name="path">The full path of an existing file.</param>
	/// <returns>
	/// <see langword="true"/> when <c>access(path, X_OK)</c> succeeds, or when the C library cannot be called, which
	/// keeps the file as a candidate the way the search did before it checked permissions.
	/// </returns>
	/// <remarks>
	/// <c>access</c> answers for the caller, as a shell's <c>PATH</c> search does. Testing for any execute bit instead
	/// would accept a <c>0700</c> file another user owns, and <c>File.GetUnixFileMode</c> does not exist below .NET 7,
	/// so the net5.0, net6.0 and netstandard builds had no check at all (ktsu-dev/RunCommand#139).
	/// </remarks>
	internal static bool CanExecute(string path)
	{
		try
		{
			return Access(path, ExecuteOk) == 0;
		}
		catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
		{
			return true;
		}
	}

	[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
	[DllImport("libc", EntryPoint = "access", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
	private static extern int Access(string path, int mode);
}
