namespace ktsu.RunCommand;

/// <summary>
/// Stream reader for StandardOutput and StandardError stream readers.
/// Runs a BeginRead loop on the underlying stream until the stream is closed.
/// The OnDataReceived event sends data received on the stream in non delimited chunks.
/// The event subscriber can then split on newline characters etc. as desired.
/// </summary>
/// <remarks>
/// This class utilizes a primary constructor that accepts a <see cref="StreamReader"/> instance.
/// </remarks>
/// <param name="streamReader">The StreamReader to use for asynchronous reading.</param>
public class AsyncStreamReader(StreamReader streamReader)
{
	/// <summary>
	/// Event triggered when data is received from the stream.
	/// </summary>
	public event EventHandler<AsyncStreamDataReceivedEventArgs>? OnDataReceived;

	private readonly byte[] buffer = new byte[4096];

	/// <summary>
	/// Initializes a new instance of the <see cref="AsyncStreamReader"/> class with an event handler.
	/// </summary>
	/// <param name="streamReader">The StreamReader to use for asynchronous reading.</param>
	/// <param name="onDataReceived">The event handler to invoke when data is received.</param>
	public AsyncStreamReader(StreamReader streamReader, EventHandler<AsyncStreamDataReceivedEventArgs>? onDataReceived)
		: this(streamReader) => OnDataReceived = onDataReceived;

	/// <summary>
	/// Starts the asynchronous reading process.
	/// </summary>
	public void Start() => Read();

	private void Read()
	{
		if (OnDataReceived is not null)
		{
			streamReader.BaseStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), null);
		}
	}

	/// <summary>
	/// Callback method invoked when data is read from the stream.
	/// </summary>
	/// <param name="asyncResult">The result of the asynchronous read operation.</param>
	private void ReadCallback(IAsyncResult asyncResult)
	{
		int bytesRead = streamReader.BaseStream.EndRead(asyncResult);

		ArgumentNullException.ThrowIfNull(OnDataReceived);

		if (bytesRead > 0)
		{
			string data = streamReader.CurrentEncoding.GetString(buffer, 0, bytesRead);
			OnDataReceived.Invoke(this, new AsyncStreamDataReceivedEventArgs(data));
		}

		if (!streamReader.EndOfStream)
		{
			Read();
		}
	}
}
