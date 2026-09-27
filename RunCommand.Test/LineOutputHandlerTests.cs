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
		Assert.AreEqual("Incomplete", handler.outputBuffer);
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
		Assert.AreEqual("Incomplete", handler.errorBuffer);
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
		Assert.AreEqual(string.Empty, handler.outputBuffer);
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
		Assert.AreEqual(string.Empty, handler.errorBuffer);
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
		Assert.AreEqual("x\r", handler.outputBuffer);
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
		Assert.AreEqual("e", handler.outputBuffer);
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
		Assert.AreEqual("", handler.outputBuffer);
		Assert.AreEqual("", handler.errorBuffer);
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
}
