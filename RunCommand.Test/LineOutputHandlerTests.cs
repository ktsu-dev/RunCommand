// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RunCommand.Test;

[TestClass]
public class LineOutputHandlerTests
{
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
}
