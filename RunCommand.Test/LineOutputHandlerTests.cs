// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand.Test;

[TestClass]
public class LineOutputHandlerTests
{
	private static readonly string[] ExpectedXY = ["x", "y"];
	private static readonly string[] ExpectedEveryLineEndingKind = ["a", "b", "c", "d", ""];
	private static readonly string[] ExpectedALast = ["a", "last"];
	private static readonly string[] ExpectedALastNext = ["a", "last", "next"];
	private static readonly string[] ExpectedOops = ["oops"];
	private static readonly string[] ExpectedCrlfAcrossReads = ["a", "b", "", "c"];

	[TestMethod]
	public void HandleStandardOutputDataShouldProcessLinesCorrectly()
	{
		// Arrange
		string data = "Line1\nLine2\nLine3\n";
		string[] expectedLines = ["Line1", "Line2", "Line3"];
		int index = 0;

		LineOutputHandler handler = new(
			onStandardOutput: line =>
			{
				Assert.AreEqual(expectedLines[index], line);
				index++;
			});

		// Act
		handler.HandleStandardOutputData(data);

		// Assert
		Assert.AreEqual(expectedLines.Length, index);
	}

	[TestMethod]
	public void HandleStandardErrorDataShouldProcessLinesCorrectly()
	{
		// Arrange
		string data = "Error1\nError2\nError3\n";
		string[] expectedLines = ["Error1", "Error2", "Error3"];
		int index = 0;

		LineOutputHandler handler = new(
			onStandardError: line =>
			{
				Assert.AreEqual(expectedLines[index], line);
				index++;
			});

		// Act
		handler.HandleStandardErrorData(data);

		// Assert
		Assert.AreEqual(expectedLines.Length, index);
	}

	[TestMethod]
	public void HandleStandardOutputDataShouldBufferIncompleteLines()
	{
		// Arrange
		string data = "Line1\nLine2\nIncomplete";
		string[] expectedLines = ["Line1", "Line2"];
		int index = 0;

		LineOutputHandler handler = new(
			onStandardOutput: line =>
			{
				Assert.AreEqual(expectedLines[index], line);
				index++;
			});

		// Act
		handler.HandleStandardOutputData(data);

		// Assert
		Assert.AreEqual(expectedLines.Length, index);
		Assert.AreEqual("Incomplete", handler.outputBuffer.ToString());
	}

	[TestMethod]
	public void HandleStandardErrorDataShouldBufferIncompleteLines()
	{
		// Arrange
		string data = "Error1\nError2\nIncomplete";
		string[] expectedLines = ["Error1", "Error2"];
		int index = 0;

		LineOutputHandler handler = new(
			onStandardError: line =>
			{
				Assert.AreEqual(expectedLines[index], line);
				index++;
			});

		// Act
		handler.HandleStandardErrorData(data);

		// Assert
		Assert.AreEqual(expectedLines.Length, index);
		Assert.AreEqual("Incomplete", handler.errorBuffer.ToString());
	}

	[TestMethod]
	public void HandleStandardOutputDataShouldTreatCrlfSplitAcrossChunksAsOneLineBreak()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);

		// Act
		handler.HandleStandardOutputData("x\r");
		handler.HandleStandardOutputData("\ny\n");

		// Assert
		Assert.AreSequenceEqual(ExpectedXY, lines);
		Assert.AreEqual(string.Empty, handler.outputBuffer.ToString());
	}

	[TestMethod]
	public void HandleStandardErrorDataShouldTreatCrlfSplitAcrossChunksAsOneLineBreak()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardError: lines.Add);

		// Act
		handler.HandleStandardErrorData("x\r");
		handler.HandleStandardErrorData("\ny\n");

		// Assert
		Assert.AreSequenceEqual(ExpectedXY, lines);
		Assert.AreEqual(string.Empty, handler.errorBuffer.ToString());
	}

	[TestMethod]
	public void HandleStandardOutputDataShouldHoldBackTrailingCarriageReturn()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);

		// Act
		handler.HandleStandardOutputData("x\r");

		// Assert
		Assert.IsEmpty(lines);
		Assert.AreEqual("x\r", handler.outputBuffer.ToString());
	}

	[TestMethod]
	public void HandleStandardOutputDataShouldSplitOnEveryLineEndingKind()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);

		// Act
		handler.HandleStandardOutputData("a\r\nb\rc\nd\r\n\r\ne");

		// Assert
		Assert.AreSequenceEqual(ExpectedEveryLineEndingKind, lines);
		Assert.AreEqual("e", handler.outputBuffer.ToString());
	}

	[TestMethod]
	public void CompleteShouldDeliverTheFinalUnterminatedLine()
	{
		// Arrange
		List<string> output = [];
		List<string> error = [];
		LineOutputHandler handler = new(onStandardOutput: output.Add, onStandardError: error.Add);
		handler.HandleStandardOutputData("a\nlast");
		handler.HandleStandardErrorData("oops");

		// Act
		handler.Complete();

		// Assert
		Assert.AreSequenceEqual(ExpectedALast, output);
		Assert.AreSequenceEqual(ExpectedOops, error);
		Assert.AreEqual("", handler.outputBuffer.ToString());
		Assert.AreEqual("", handler.errorBuffer.ToString());
	}

	[TestMethod]
	public void CompleteShouldNotEmitAnExtraLineWhenOutputEndsOnALineBreak()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);
		handler.HandleStandardOutputData("a\nlast\n");

		// Act
		handler.Complete();

		// Assert
		Assert.AreSequenceEqual(ExpectedALast, lines);
	}

	[TestMethod]
	public void CompleteShouldTreatATrailingCarriageReturnAsALineBreak()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);
		handler.HandleStandardOutputData("a\rlast\r");

		// Act
		handler.Complete();

		// Assert
		Assert.AreSequenceEqual(ExpectedALast, lines);
	}

	[TestMethod]
	public void CompleteShouldKeepAPartialLineFromLeakingIntoTheNextRun()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);
		handler.HandleStandardOutputData("a\nlast");
		handler.Complete();

		// Act
		handler.HandleStandardOutputData("next\n");

		// Assert
		Assert.AreSequenceEqual(ExpectedALastNext, lines);
	}

	[TestMethod]
	public void HandleStandardOutputDataShouldTreatALineBreakSplitAcrossReadsAsOne()
	{
		// Arrange
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);

		// Act
		handler.HandleStandardOutputData("a\r");
		handler.HandleStandardOutputData("\nb\r");
		handler.HandleStandardOutputData("\r");
		handler.HandleStandardOutputData("c\n");

		// Assert
		Assert.AreSequenceEqual(ExpectedCrlfAcrossReads, lines);
		Assert.AreEqual("", handler.outputBuffer.ToString());
	}

	[TestMethod]
	public void HandleStandardOutputDataShouldBeLinearInTheLengthOfALongLine()
	{
		// Arrange
		const int chunkCount = 1024;
		const int chunkLength = 4096;
		string chunk = new('a', chunkLength);
		List<string> lines = [];
		LineOutputHandler handler = new(onStandardOutput: lines.Add);
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		// Act
		for (int i = 0; i < chunkCount; i++)
		{
			handler.HandleStandardOutputData(chunk);
		}

		handler.HandleStandardOutputData("\n");
		stopwatch.Stop();

		// Assert
		Assert.HasCount(1, lines);
		Assert.AreEqual(chunkCount * chunkLength, lines[0].Length);
		Assert.IsLessThan(TimeSpan.FromSeconds(2), stopwatch.Elapsed, $"4 MB in 4 KB reads took {stopwatch.Elapsed}");
	}
}
