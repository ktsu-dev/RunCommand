namespace ktsu.RunCommand;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Provides functionality to execute commands and process their outputs.
/// </summary>
public static class RunCommand
{
	/// <summary>
	/// Executes the specified command.
	/// </summary>
	/// <param name="command">The command to execute, including any arguments.</param>
	public static void Execute(string command) => Execute(command, null, null);

	/// <summary>
	/// Executes the specified command and processes the standard output.
	/// </summary>
	/// <param name="command">The command to execute, including any arguments.</param>
	/// <param name="onStandardOutput">An action to be invoked with undelimeted chunks from the command's standard output stream.</param>
	public static void Execute(string command, Action<string>? onStandardOutput) => Execute(command, onStandardOutput, null);

	/// <summary>
	/// Executes the specified command and processes both standard output and standard error.
	/// </summary>
	/// <param name="command">The command to execute, including any arguments.</param>
	/// <param name="onStandardOutput">An action to be invoked with undelimeted chunks from the command's standard output stream.</param>
	/// <param name="onStandardError">An action to be invoked with undelimeted chunks from the command's standard error stream.</param>
	public static void Execute(string command, Action<string>? onStandardOutput, Action<string>? onStandardError)
		=> ExecuteAsync(command, onStandardOutput, onStandardError).Wait();

	/// <summary>
	/// Asynchronously executes the specified command.
	/// </summary>
	/// <param name="command">The command to execute, including any arguments.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static async Task ExecuteAsync(string command)
		=> await ExecuteAsync(command, null, null).ConfigureAwait(false);

	/// <summary>
	/// Asynchronously executes the specified command and processes the standard output.
	/// </summary>
	/// <param name="command">The command to execute, including any arguments.</param>
	/// <param name="onStandardOutput">An action to be invoked with undelimeted chunks from the command's standard output stream.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static async Task ExecuteAsync(string command, Action<string>? onStandardOutput)
		=> await ExecuteAsync(command, onStandardOutput, null).ConfigureAwait(false);

	/// <summary>
	/// Asynchronously executes the specified command and processes both standard output and standard error.
	/// </summary>
	/// <param name="command">The command to execute, including any arguments.</param>
	/// <param name="onStandardOutput">An action to be invoked with undelimeted chunks from the command's standard output stream.</param>
	/// <param name="onStandardError">An action to be invoked with undelimeted chunks from the command's standard error stream.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static async Task ExecuteAsync(string command, Action<string>? onStandardOutput, Action<string>? onStandardError)
	{
		ArgumentNullException.ThrowIfNull(command);

		string[] commandParts = command.Split(' ', 2);

		string filename = commandParts[0];
		string arguments = commandParts.Length > 1 ? commandParts[1] : string.Empty;

		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = filename,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				UseShellExecute = false,
				CreateNoWindow = true,
			}
		};

		bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		if (isWindows)
		{
			process.StartInfo.LoadUserProfile = true;
		}

		process.Start();

		AsyncStreamReader standardOutputReader = new(process.StandardOutput, onStandardOutput is null ? null : (sender, args) => onStandardOutput.Invoke(args.Data));
		AsyncStreamReader standardErrorReader = new(process.StandardError, onStandardError is null ? null : (sender, args) => onStandardError.Invoke(args.Data));
		standardOutputReader.Start();
		standardErrorReader.Start();

		await process.WaitForExitAsync().ConfigureAwait(false);
	}
}
