public class LobbyPlayerData
{
	public long PeerId { get; }
	public string PlayerName { get; }
	public bool IsHost { get; }
	public bool IsReady { get; }

	public LobbyPlayerData(
		long peerId,
		string playerName,
		bool isHost,
		bool isReady
	)
	{
		PeerId = peerId;
		PlayerName = playerName;
		IsHost = isHost;
		IsReady = isReady;
	}
}
