// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand.Test;

using System.Runtime.InteropServices;

[TestClass]
public class LineOutputHandlerRunTests
{
	private static readonly string[] ExpectedALast = ["a", "last"];
	private static readonly string[] ExpectedALastNext = ["a", "last", "next"];

	/// <summary>
	/// Returns a command that prints a file's contents unchanged.
	/// </summary>
	private static (string FileName, string[] Arguments) GetPrintFileCommand(string path) =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("cmd", ["/c", "type", path])
			: ("cat", [path]);

	[TestMethod]
	public async Task LineOutputHandlerShouldDeliverAFinalLineWithoutALineBreakAndNotCarryItIntoTheNextRun()
	{
		string first = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)}.{nameof(LineOutputHandlerShouldDeliverAFinalLineWithoutALineBreakAndNotCarryItIntoTheNextRun)}.1.txt");
		string second = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)}.{nameof(LineOutputHandlerShouldDeliverAFinalLineWithoutALineBreakAndNotCarryItIntoTheNextRun)}.2.txt");
		await File.WriteAllTextAsync(first, "a\nlast").ConfigureAwait(false);
		await File.WriteAllTextAsync(second, "next\n").ConfigureAwait(false);

		try
		{
			List<string> lines = [];
			LineOutputHandler handler = new(onStandardOutput: lines.Add);

			(string fileName, string[] arguments) = GetPrintFileCommand(first);
			int exitCode = await RunCommand.ExecuteAsync(fileName, arguments, handler).ConfigureAwait(false);
			Assert.AreEqual(0, exitCode);
			Assert.AreSequenceEqual(ExpectedALast, lines);

			(fileName, arguments) = GetPrintFileCommand(second);
			exitCode = await RunCommand.ExecuteAsync(fileName, arguments, handler).ConfigureAwait(false);
			Assert.AreEqual(0, exitCode);
			Assert.AreSequenceEqual(ExpectedALastNext, lines);
		}
		finally
		{
			File.Delete(first);
			File.Delete(second);
		}
	}

	[TestMethod]
	public async Task ReusedLineOutputHandlerShouldDropTheLineACancelledRunLeftUnfinished()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs sh to print a line without a line break. The reset this covers is in platform independent code, so the other legs cover it.");
		}

		List<string> lines = [];
		using SemaphoreSlim partialArrived = new(0);
		LineOutputHandler handler = new(lines.Add);

		using (CancellationTokenSource cancellationTokenSource = new())
		{
			Task<int> cancelled = RunCommand.ExecuteAsync(
				"sh",
				["-c", "printf partial; sleep 30"],
				new SignallingLineOutputHandler(handler, partialArrived),
				cancellationTokenSource.Token);

			Assert.IsTrue(await partialArrived.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false), "Expected the partial line to arrive.");
			await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
			await Assert.ThrowsAsync<OperationCanceledException>(() => cancelled).ConfigureAwait(false);
		}

		Assert.AreEqual("partial", handler.outputBuffer.ToString(), "The cancelled run should have left its partial line buffered.");

		int exitCode = await RunCommand.ExecuteAsync("sh", ["-c", "echo hello"], handler).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode);
		Assert.AreEqual("hello", string.Join(" | ", lines), "The next run's first line should not carry the cancelled run's leftover text.");
	}

	[TestMethod]
	public async Task LineOutputHandlerSharedByOverlappingRunsShouldKeepEachRunsLinesApart()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs sh to print part of a line, pause, then finish it. The per-run splitting this covers is platform independent, so the other legs cover it.");
		}

		// ktsu-dev/RunCommand#130: the second run's Reset threw away the first run's partial line, and
		// both runs then appended to the same buffer, so their text came out joined into one line.
		List<string> lines = [];
		using SemaphoreSlim firstPartArrived = new(0);
		LineOutputHandler handler = new(line =>
		{
			lock (lines)
			{
				lines.Add(line);
			}
		});

		Task<int> first = RunCommand.ExecuteAsync(
			"sh",
			["-c", "printf AAAA; sleep 1; printf 'AAAA\\n'"],
			new SignallingLineOutputHandler(handler, firstPartArrived));

		Assert.IsTrue(await firstPartArrived.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false), "Expected the first run's partial line to arrive.");

		Task<int> second = RunCommand.ExecuteAsync("sh", ["-c", "printf BBBB; sleep 0.2; printf 'BBBB\\n'"], handler);

		int[] exitCodes = await Task.WhenAll(first, second).ConfigureAwait(false);

		Assert.AreEqual(0, exitCodes[0], "The first run should succeed.");
		Assert.AreEqual(0, exitCodes[1], "The second run should succeed.");
		List<string> sorted;
		lock (lines)
		{
			sorted = [.. lines.Order(StringComparer.Ordinal)];
		}

		Assert.AreEqual("AAAAAAAA | BBBBBBBB", string.Join(" | ", sorted));
	}

	[TestMethod]
	public async Task ReusedLineOutputHandlerShouldDropTheLineARunWhoseCallbackThrewLeftUnfinished()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs sh to print a line without a line break. The reset this covers is in platform independent code, so the other legs cover it.");
		}

		List<string> lines = [];
		bool throwOnError = true;
		LineOutputHandler handler = new(
			lines.Add,
			line =>
			{
				if (throwOnError)
				{
					throw new InvalidOperationException("handler failed");
				}
			});

		// Standard output's partial line is buffered before standard error's line makes the callback throw.
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => RunCommand.ExecuteAsync("sh", ["-c", "printf partial; sleep 0.5; echo boom >&2; sleep 30"], handler)).ConfigureAwait(false);

		// The failed run's partial line stays in that run's own handler (#130), so nothing is left on this one.
		Assert.AreEqual(string.Empty, handler.outputBuffer.ToString(), "A run should split lines in its own handler, not the shared one.");

		throwOnError = false;
		int exitCode = await RunCommand.ExecuteAsync("sh", ["-c", "echo hello"], handler).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode);
		Assert.AreEqual("hello", string.Join(" | ", lines), "The next run's first line should not carry the failed run's leftover text.");
	}

	[TestMethod]
	public async Task AbandonedReaderShouldNotDeliverIntoALaterRunOnTheSameHandler()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs a shell that can orphan a child out of its own process tree while that child keeps the pipe it inherited. That a cancelled run's reads stop delivering is platform independent, so the other legs cover it.");
		}

		List<string> lines = [];
		using SemaphoreSlim partialArrived = new(0);
		LineOutputHandler handler = new(lines.Add);

		// The orphaned subshell escapes the kill and keeps the cancelled run's standard output open, then
		// writes to it a second later, while the next run on the same handler is still going. The
		// cancelled run gave up on that read, so what arrives there must not reach the handler.
		using (CancellationTokenSource cancellationTokenSource = new())
		{
			Task<int> cancelled = RunCommand.ExecuteAsync(
				"sh",
				["-c", "sh -c '(sleep 1; printf late) &'; printf partial; sleep 30"],
				new SignallingLineOutputHandler(handler, partialArrived),
				cancellationTokenSource.Token);

			Assert.IsTrue(await partialArrived.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false), "Expected the partial line to arrive.");
			await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
			await Assert.ThrowsAsync<OperationCanceledException>(() => cancelled).ConfigureAwait(false);
		}

		int exitCode = await RunCommand.ExecuteAsync("sh", ["-c", "sleep 2; echo hello"], handler).ConfigureAwait(false);

		Assert.AreEqual(0, exitCode);
		Assert.AreEqual("hello", string.Join(" | ", lines), "Output the cancelled run's abandoned reader received should not reach the later run.");
	}

	/// <summary>
	/// Forwards standard output to <paramref name="inner"/> and signals <paramref name="received"/> after each chunk,
	/// so a test can wait for output to arrive before it cancels.
	/// </summary>
	private sealed class SignallingLineOutputHandler(LineOutputHandler inner, SemaphoreSlim received) : OutputHandler
	{
		internal override void HandleStandardOutputData(string data)
		{
			inner.HandleStandardOutputData(data);
			received.Release();
		}
	}
}
