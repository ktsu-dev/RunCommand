// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand.Test;

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices;

[TestClass]
public class OutputDecodingTests
{
	/// <summary>
	/// Returns a command that writes a file's bytes to standard output unchanged, as an executable
	/// plus separate arguments.
	/// </summary>
	/// <remarks>
	/// Windows has no reliably byte-faithful built-in for this. <c>cmd /c type</c> looked like one
	/// but transcodes a file carrying a UTF-16 byte order mark instead of copying it, which is
	/// exactly the input these tests need, so PowerShell writes the raw bytes to the standard
	/// output stream instead. <see cref="EmitsBytesFaithfully"/> checks the result rather than
	/// trusting it.
	/// </remarks>
	internal static (string FileName, string[] Arguments) GetEmitFileBytesCommand(string path) =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? ("powershell", [
				"-NoProfile",
				"-NonInteractive",
				"-Command",
				$"$b=[IO.File]::ReadAllBytes('{path}'); $s=[Console]::OpenStandardOutput(); $s.Write($b,0,$b.Length); $s.Flush()"])
			: ("cat", [path]);

	private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

	// Runs the emitter through the library, so the test controls the exact bytes that reach the
	// pipe and can assert on what the library makes of them.
	private static string RunOverPath(string path, Encoding encoding)
	{
		StringBuilder output = new();
		(string fileName, string[] arguments) = GetEmitFileBytesCommand(path);
		_ = RunCommand.Execute(fileName, arguments, new OutputHandler(o => output.Append(o), null, encoding));

		return output.ToString();
	}

	/// <summary>
	/// Checks that this platform's emitter really does put <paramref name="bytes"/> on the pipe
	/// unchanged, so that a mangling emitter reports itself instead of being read as a result about
	/// the library.
	/// </summary>
	/// <remarks>
	/// This drives the emitter through <see cref="Process"/> directly and copies the raw
	/// <see cref="StreamReader.BaseStream"/>, deliberately never touching the code under test. A
	/// control that went through <see cref="RunCommand"/> would measure the very defect these tests
	/// exist to catch and blame the emitter for it, which would turn a regression into an
	/// inconclusive result instead of a failure.
	/// </remarks>
	private static bool EmitsBytesFaithfully(string path, byte[] bytes, out string diagnostic)
	{
		byte[] actual;

		try
		{
			(string fileName, string[] arguments) = GetEmitFileBytesCommand(path);
			ProcessStartInfo startInfo = new()
			{
				FileName = fileName,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			foreach (string argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}

			using Process process = Process.Start(startInfo)!;
			using MemoryStream captured = new();
			process.StandardOutput.BaseStream.CopyTo(captured);
			process.WaitForExit();
			actual = captured.ToArray();
		}
		catch (System.ComponentModel.Win32Exception ex)
		{
			diagnostic = $"This platform's byte emitter could not be started: {ex.Message}";
			return false;
		}

		bool faithful = actual.SequenceEqual(bytes);
		diagnostic = faithful
			? string.Empty
			: "This platform's byte emitter altered the bytes, so the library cannot be judged from them. "
				+ $"Expected [{Convert.ToHexString(bytes)}], the pipe carried [{Convert.ToHexString(actual)}].";

		return faithful;
	}

	// Writes the bytes to a file of this test's own and confirms the platform can actually put them
	// on a pipe unchanged, returning the path to feed to the library.
	private static string WriteBytesForTest(byte[] bytes, string caller)
	{
		string path = Path.Join(RunCommandTests.CreateDirectoryForTest(caller), "bytes.bin");
		File.WriteAllBytes(path, bytes);

		if (!EmitsBytesFaithfully(path, bytes, out string diagnostic))
		{
			Assert.Inconclusive(diagnostic);
		}

		return path;
	}

	private static void AssertDecodeFailure(byte[] bytes, [CallerMemberName] string caller = "")
	{
		// The emitter check has to happen out here: an Assert.Inconclusive raised inside the lambda
		// below would be caught by Assert.ThrowsExactly and reported as a failing test.
		string path = WriteBytesForTest(bytes, caller);

		AggregateException thrown = Assert.ThrowsExactly<AggregateException>(() => RunOverPath(path, StrictUtf8));
		Assert.IsTrue(
			thrown.Flatten().InnerExceptions.Any(e => e is DecoderFallbackException),
			$"Expected a decode failure, got: {thrown}");
	}

	[TestMethod]
	public void OutputEncodingIsNotReplacedByAUtf16ByteOrderMark() =>
		// "hi" in UTF-16LE behind its byte order mark. Process builds its StandardOutput reader with
		// byte-order-mark detection on, which used to switch the reader to UTF-16LE and return "hi"
		// even though the caller asked for strict UTF-8 and these are not valid UTF-8 bytes.
		AssertDecodeFailure([0xFF, 0xFE, (byte)'h', 0x00, (byte)'i', 0x00]);

	[TestMethod]
	public void OutputThatIsOnlyAUtf16ByteOrderMarkIsNotReportedAsSuccess() =>
		// The same detection consumed a lone FF FE as a byte order mark, leaving nothing to decode,
		// so a strict encoding reported no output and no error for bytes it should have rejected.
		AssertDecodeFailure([0xFF, 0xFE]);

	[TestMethod]
	public void AUtf8ByteOrderMarkIsStrippedFromTheStartOfOutput()
	{
		// A byte order mark matching the requested encoding is still dropped, so turning the
		// detection off did not start leaking U+FEFF into captured output.
		byte[] bytes = [0xEF, 0xBB, 0xBF, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o'];
		string path = WriteBytesForTest(bytes, nameof(AUtf8ByteOrderMarkIsStrippedFromTheStartOfOutput));

		Assert.AreEqual("hello", RunOverPath(path, StrictUtf8));
	}

	/// <summary>
	/// Returns the encoding a byte-order-mark test names, along with that encoding's own BOM.
	/// </summary>
	/// <remarks>
	/// Named rather than passed because <see cref="DataRowAttribute"/> only takes constants. The
	/// cases split on whether the encoding has a preamble: <see cref="StreamReader"/> skips a
	/// matching preamble itself whatever detectEncodingFromByteOrderMarks says, so one with a
	/// preamble must not be stripped a second time by the library.
	/// </remarks>
	private static (Encoding Encoding, byte[] Bom) GetByteOrderMarkCase(string name) => name switch
	{
		"Encoding.UTF8" => (Encoding.UTF8, [0xEF, 0xBB, 0xBF]),
		"UTF8Encoding(false)" => (new UTF8Encoding(false), [0xEF, 0xBB, 0xBF]),
		"Encoding.Unicode" => (Encoding.Unicode, [0xFF, 0xFE]),
		"UnicodeEncoding(false, false)" => (new UnicodeEncoding(bigEndian: false, byteOrderMark: false), [0xFF, 0xFE]),
		_ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown encoding case."),
	};

	[TestMethod]
	[DataRow("Encoding.UTF8")]
	[DataRow("UTF8Encoding(false)")]
	[DataRow("Encoding.Unicode")]
	[DataRow("UnicodeEncoding(false, false)")]
	public void OnlyTheByteOrderMarkIsStrippedWhenARealU_FEFFFollowsIt(string encodingName)
	{
		// A BOM, then U+FEFF that is part of the output, then "A". The BOM goes and the content
		// stays, whichever of two equivalent encodings the caller passed. With a preamble the reader
		// dropped the BOM and the library then dropped the real U+FEFF after it.
		(Encoding encoding, byte[] bom) = GetByteOrderMarkCase(encodingName);
		byte[] bytes = [.. bom, .. encoding.GetBytes("\uFEFFA")];
		string path = WriteBytesForTest(bytes, $"{nameof(OnlyTheByteOrderMarkIsStrippedWhenARealU_FEFFFollowsIt)}.{new string([.. encodingName.Where(char.IsLetterOrDigit)])}");

		string output = RunOverPath(path, encoding);

		Assert.AreEqual("FEFF 0041", string.Join(' ', output.Select(c => ((int)c).ToString("X4", CultureInfo.InvariantCulture))));
	}

	[TestMethod]
	[DataRow("Encoding.UTF8")]
	[DataRow("UTF8Encoding(false)")]
	[DataRow("Encoding.Unicode")]
	[DataRow("UnicodeEncoding(false, false)")]
	public void ALoneByteOrderMarkIsStrippedWhicheverEncodingReadsIt(string encodingName)
	{
		(Encoding encoding, byte[] bom) = GetByteOrderMarkCase(encodingName);
		byte[] bytes = [.. bom, .. encoding.GetBytes("hello")];
		string path = WriteBytesForTest(bytes, $"{nameof(ALoneByteOrderMarkIsStrippedWhicheverEncodingReadsIt)}.{new string([.. encodingName.Where(char.IsLetterOrDigit)])}");

		Assert.AreEqual("hello", RunOverPath(path, encoding));
	}

	[TestMethod]
	public void ADecodeFailureIsNotDiscardedWhenTheProcessKeepsRunning()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Assert.Inconclusive("Needs a shell that can emit bytes and then stay alive. The race this covers is in platform independent code, so the other legs cover it.");
		}

		// The read loop stops when the process exits, so a read that faults while the process is
		// still running used to be replaced by a fresh read on the next pass and its decode failure
		// thrown away. Sleeping after the bad byte keeps the process alive long enough for that pass
		// to happen, which makes the race deterministic rather than roughly one run in six.
		string path = Path.Join(RunCommandTests.CreateDirectoryForTest(), "bytes.bin");
		File.WriteAllBytes(path, [(byte)'o', (byte)'k', 0xFF]);

		StringBuilder output = new();
		AggregateException thrown = Assert.ThrowsExactly<AggregateException>(
			() => RunCommand.Execute(
				"sh",
				["-c", $"cat '{path}'; sleep 1"],
				new OutputHandler(o => output.Append(o), null, StrictUtf8)));

		Assert.IsTrue(
			thrown.Flatten().InnerExceptions.Any(e => e is DecoderFallbackException),
			$"Expected a decode failure, got: {thrown}");
	}
}
