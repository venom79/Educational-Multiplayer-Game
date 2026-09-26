public class NetworkPlayer
{
    public long PeerId { get; }

    public string PlayerName { get; private set; }

    public bool IsHost { get; }

    public bool IsReady { get; private set; }

    public NetworkPlayer(
        long peerId,
        string playerName,
        bool isHost
    )
    {
        PeerId = peerId;
        PlayerName = playerName;
        IsHost = isHost;
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
}