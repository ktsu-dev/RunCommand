// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RunCommand.Test;

[TestClass]
public class AsyncProcessStreamReaderTests
{
	private const string Emoji = "\U0001F600";
	private static readonly string[] ExpectedOneCharChunks = ["x", Emoji, Emoji, "y", Emoji];
	private static readonly string[] ExpectedPlainChunks = ["abcd", "ef"];

	private static async Task<List<string>> ReadChunksAsync(string text, int bufferSize)
	{
		List<string> chunks = [];
		using StringReader reader = new(text);
		await AsyncProcessStreamReader.ReadChunks(reader, new char[bufferSize], chunks.Add).ConfigureAwait(false);
		return chunks;
	}

	private static void AssertNoChunkSplitsASurrogatePair(List<string> chunks)
	{
		for (int i = 0; i < chunks.Count; i++)
		{
			Assert.IsFalse(char.IsHighSurrogate(chunks[i][^1]), $"Chunk {i} ends with a lone high surrogate.");
			Assert.IsFalse(char.IsLowSurrogate(chunks[i][0]), $"Chunk {i} starts with a lone low surrogate.");
		}
	}

	[TestMethod]
	public async Task ASurrogatePairOnABufferBoundaryIsDeliveredInOneChunk()
	{
		// The issue's reproduction: 4095 chars fill all but the last slot of the 4096-char buffer, so
		// the read stops between the emoji's two halves.
		string text = new string('a', 4095) + Emoji + new string('b', 5000);

		List<string> chunks = await ReadChunksAsync(text, 4096).ConfigureAwait(false);

		AssertNoChunkSplitsASurrogatePair(chunks);
		Assert.AreEqual(text, string.Concat(chunks));
	}

	[TestMethod]
	public async Task EverySurrogatePairSurvivesAOneCharBuffer()
	{
		string text = $"x{Emoji}{Emoji}y{Emoji}";

		List<string> chunks = await ReadChunksAsync(text, 1).ConfigureAwait(false);

		AssertNoChunkSplitsASurrogatePair(chunks);
		CollectionAssert.AreEqual(ExpectedOneCharChunks, chunks);
	}

	[TestMethod]
	public async Task AHighSurrogateAtEndOfStreamIsStillDelivered()
	{
		// Nothing follows it, so holding it back must not lose it.
		string text = "ab\uD83D";

		List<string> chunks = await ReadChunksAsync(text, 4096).ConfigureAwait(false);

		Assert.AreEqual(text, string.Concat(chunks));
	}

	[TestMethod]
	public async Task OutputWithoutSurrogatesIsDeliveredAsItIsRead()
	{
		List<string> chunks = await ReadChunksAsync("abcdef", 4).ConfigureAwait(false);

		CollectionAssert.AreEqual(ExpectedPlainChunks, chunks);
	}
}
