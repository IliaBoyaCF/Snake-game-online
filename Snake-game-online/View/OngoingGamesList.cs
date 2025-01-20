using Network.Node;
using SnakeOnline;
using SnakeOnline.Game.States;
using Snakes;
using System.Net;

namespace Network;

public class OngoingGamesList
{

    public const int s_announcementExpirationPeriodMs = 1_000;

    public delegate void OnOngoingGamesListUpdate(List<Tuple<IPEndPoint, GameMessage>> games);

    public event OnOngoingGamesListUpdate? Update;

    private record GameRecord(GameMessage AnnouncementMessage, long ReceivedTime);

    private readonly Dictionary<Tuple<IPEndPoint, string>, GameRecord> _ongoingGames = [];

    public record GameInfo(string Name, List<IPlayerState> Players, IGameInfo.IGameConfig GameConfig,
        bool CanJoin, IPEndPoint sender) : IGameInfo;

    private Timer? _updateTimer;

    public class GameConfig : IGameInfo.IGameConfig
    {
        private readonly Snakes.GameConfig _config;

        public GameConfig(Snakes.GameConfig gameConfig)
        {
            _config = gameConfig;
        }

        public int FieldWidth => _config.Width;

        public int FieldHeight => _config.Height;

        public int FoodStatic => _config.FoodStatic;

        public int StateDelay_ms => _config.StateDelayMs;
    }

    private readonly object _lock = new object();

    private readonly SocketWrapper _socketWrapper;

    public OngoingGamesList(SocketWrapper socketWrapper)
    {
        _socketWrapper = socketWrapper;
    }

    public void Start()
    {
        _ongoingGames.Clear();
        _socketWrapper.OnMulticastMessageReceived += OnMulticastMessageReceive;
        _updateTimer = new Timer((_) => UpdateRoutine(), null, 0, s_announcementExpirationPeriodMs / 2);
    }

    public void Stop()
    {
        _updateTimer?.Dispose();
        _updateTimer = null;
        _socketWrapper.OnMulticastMessageReceived -= OnMulticastMessageReceive;
    }

    private void UpdateRoutine()
    {
        List<Tuple<IPEndPoint, string>> expiredGameAnnouncements = [];
        lock (_lock)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var pair in _ongoingGames)
            {
                if (currentTime - pair.Value.ReceivedTime > s_announcementExpirationPeriodMs)
                {
                    expiredGameAnnouncements.Add(pair.Key);
                }
            }
            foreach (var key in expiredGameAnnouncements)
            {
                _ongoingGames.Remove(key);
            }
        }
        if (expiredGameAnnouncements.Count > 0)
        {
            Update?.Invoke(OngoingGames());
        }
    }


    public void OnMulticastMessageReceive(IPEndPoint sender, GameMessage message)
    {
        if (message.TypeCase != GameMessage.TypeOneofCase.Announcement)
        {
            return;
        }
        GameRecord gameRecord = new GameRecord(message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Tuple<IPEndPoint, string> key = Tuple.Create(sender, message.Announcement.Games[0].GameName);
        bool ElementPresented = true;
        lock (_lock)
        {
            ElementPresented = _ongoingGames.Remove(key);
            _ongoingGames.Add(key, gameRecord);
        }
        if (!ElementPresented)
        {
            Update?.Invoke(OngoingGames());
        }
    }
    public List<Tuple<IPEndPoint, GameMessage>> OngoingGames()
    {
        List<KeyValuePair<Tuple<IPEndPoint, string>, GameRecord>> values;
        lock (_lock)
        {
            values = _ongoingGames.ToList();
        }
        return values.Select((v) => Tuple.Create(v.Key.Item1, v.Value.AnnouncementMessage)).ToList();
    }

    private GameInfo ToGameInfo(KeyValuePair<Tuple<IPEndPoint, string>, GameRecord> v)
    {
        return new GameInfo(v.Value.AnnouncementMessage.Announcement.Games[0].GameName,
            ConnectedNode.ParsePlayerStates(v.Value.AnnouncementMessage.Announcement.Games[0].Players),
            ParseGameConfig(v.Value.AnnouncementMessage.Announcement.Games[0].Config),
            v.Value.AnnouncementMessage.Announcement.Games[0].CanJoin, v.Key.Item1);
    }

    private IGameInfo.IGameConfig ParseGameConfig(Snakes.GameConfig config)
    {
        return new GameConfig(config);
    }
}
