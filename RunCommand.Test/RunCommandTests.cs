namespace ktsu.RunCommand.Test;

[TestClass]
public class RunCommandTests
{
	[TestMethod]
	public void ExecuteShouldExecuteCommand()
	{
		string tempFile = Path.GetTempFileName();
		string destinationFile = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)}.{nameof(ExecuteShouldExecuteCommand)}");

		File.Delete(destinationFile);

		string command = $"cp {tempFile} {destinationFile}";

		RunCommand.Execute(command);

		Assert.IsTrue(File.Exists(destinationFile), "Expected file to be created.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldExecuteCommand()
	{
		string tempFile = Path.GetTempFileName();
		string destinationFile = Path.Join(Path.GetTempPath(), $"{nameof(RunCommandTests)}.{nameof(ExecuteShouldExecuteCommand)}");

		File.Delete(destinationFile);

		string command = $"cp {tempFile} {destinationFile}";

		await RunCommand.ExecuteAsync(command).ConfigureAwait(false);

		Assert.IsTrue(File.Exists(destinationFile), "Expected file to be created.");
	}

	[TestMethod]
	public void ExecuteShouldCaptureStandardOutput()
	{
		var outputCollector = new List<string>();

		// Using dotnet should be available in environments with .NET installed.
		string command = "dotnet";

		RunCommand.Execute(command,
			output =>
			{
				if (!string.IsNullOrWhiteSpace(output))
				{
					outputCollector.Add(output);
				}
			});

		Assert.IsTrue(outputCollector.Count > 0, "Expected standard output to have content.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldCaptureStandardOutput()
	{
		var outputCollector = new List<string>();

		// Using dotnet should be available in environments with .NET installed.
		string command = "dotnet";

		await RunCommand.ExecuteAsync(command,
			output =>
			{
				if (!string.IsNullOrWhiteSpace(output))
				{
					outputCollector.Add(output);
				}
			}).ConfigureAwait(false);

		Assert.IsTrue(outputCollector.Count > 0, "Expected standard output to have content.");
	}

	[TestMethod]
	public void ExecuteShouldCaptureStandardOutputAndStandardError()
	{
		var outputCollector = new List<string>();
		var errorCollector = new List<string>();

		void onStandardOutput(string output)
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		}

		void onStandardError(string error)
		{
			if (!string.IsNullOrWhiteSpace(error))
			{
				errorCollector.Add(error);
			}
		}

		// Using dotnet should be available in environments with .NET installed.
		string command = "dotnet";
		RunCommand.Execute(command, onStandardOutput, onStandardError);

		Assert.IsTrue(outputCollector.Count > 0, "Expected standard output to have content.");
		Assert.AreEqual(0, errorCollector.Count, "Expected standard error to be empty.");

		outputCollector.Clear();
		errorCollector.Clear();

		// Using a command that should fail.
		command = "dotnet --versionz";
		RunCommand.Execute(command, onStandardOutput, onStandardError);

		Assert.IsTrue(outputCollector.Count > 0, "Expected standard output to have content.");
		Assert.IsTrue(errorCollector.Count > 0, "Expected standard error to have content.");
	}

	[TestMethod]
	public async Task ExecuteAsyncShouldCaptureStandardOutputAndStandardError()
	{
		var outputCollector = new List<string>();
		var errorCollector = new List<string>();

		void onStandardOutput(string output)
		{
			if (!string.IsNullOrWhiteSpace(output))
			{
				outputCollector.Add(output);
			}
		}

		void onStandardError(string error)
		{
			if (!string.IsNullOrWhiteSpace(error))
			{
				errorCollector.Add(error);
			}
		}

		// Using dotnet should be available in environments with .NET installed.
		string command = "dotnet";
		await RunCommand.ExecuteAsync(command, onStandardOutput, onStandardError).ConfigureAwait(false);

		Assert.IsTrue(outputCollector.Count > 0, "Expected standard output to have content.");
		Assert.AreEqual(0, errorCollector.Count, "Expected standard error to be empty.");

		outputCollector.Clear();
		errorCollector.Clear();

		// Using a command that should fail.
		command = "dotnet --versionz";
		await RunCommand.ExecuteAsync(command, onStandardOutput, onStandardError).ConfigureAwait(false);

		Assert.IsTrue(outputCollector.Count > 0, "Expected standard output to have content.");
		Assert.IsTrue(errorCollector.Count > 0, "Expected standard error to have content.");
	}

	[TestMethod]
	public void ExecuteShouldThrowArgumentNullExceptionWhenCommandIsNull()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			try
			{
				RunCommand.Execute(null!);
			}
			catch (AggregateException ex)
			{
				ex.Handle(static inner =>
				{
					return inner is ArgumentNullException
						? throw inner
						: false;
				});
			}
		});
	}
}
