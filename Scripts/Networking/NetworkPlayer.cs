
public enum PlayerTeam
{
	Blue,
	Red
}

public class NetworkPlayer
{
	public long PeerId { get; }

	public string PlayerName { get; private set; }

	public bool IsHost { get; }

	public bool IsReady { get; private set; }

	public PlayerTeam Team { get; private set; }

	public NetworkPlayer(
		long peerId,
		string playerName,
		bool isHost,
		PlayerTeam team = PlayerTeam.Blue
	)
	{
		PeerId = peerId;
		PlayerName = playerName;
		IsHost = isHost;
		Team = team;
		IsReady = false;
	}

	public void SetReady(bool ready)
	{
		IsReady = ready;
	}

	public void SetPlayerName(string playerName)
	{
		PlayerName = playerName;
	}

	public void SetTeam(PlayerTeam team)
	{
		Team = team;
	}
}
