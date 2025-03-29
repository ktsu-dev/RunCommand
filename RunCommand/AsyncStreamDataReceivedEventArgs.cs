namespace ktsu.RunCommand;

/// <summary>
/// Provides data for the <see cref="AsyncStreamReader.OnDataReceived"/> event.
/// </summary>
/// <remarks>
/// Contains the data retrieved from the stream.
/// </remarks>
/// <param name="data">The data retrieved from the stream.</param>
public class AsyncStreamDataReceivedEventArgs(string data) : EventArgs
{
	/// <summary>
	/// Gets the data received from the stream.
	/// </summary>
	public string Data => data;
}
