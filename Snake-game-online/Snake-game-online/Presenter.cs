using Serilog;
using Snake_game_online.model.Game.GameState;
using SnakeGameOnline.Model;
using SnakeGameOnline.Model.Network;
using SnakeGameOnline.Model.Network.Node;
using Snakes;
using System.Diagnostics;
using System.Net;
using static SnakeGameOnline.MainWindow;

namespace SnakeGameOnline;

public class Presenter
{

    public record OngoingGameInfo(string Name, List<IPlayerState> Players, 
        IGameInfo.IGameConfig GameConfig, bool CanJoin, IPEndPoint SenderAddress, GameAnnouncement GameAnnouncement) : IOngoingGameInfo;

    public class GameStateUpdatedEventArgs : EventArgs // CR: To remove
    {
        public IGameState GameState { get; }

        public GameStateUpdatedEventArgs(IGameState gameState)
        {
            GameState = gameState;
        }
    }

    public delegate void OnGameStateUpdate(object sender, GameStateUpdatedEventArgs args); // CR: Change signature to OnGameStateUpdate(IGameState state);
    
    public event OnGameStateUpdate? GameStateUpdated;

    public delegate void OnGameListUpdate(List<IOngoingGameInfo> actualList);


    public event OnGameListUpdate? GameListUpdated;

    private Model.Game.Core.Game? ongoingGame;

    private NodeContext? _nodeContext;

    private static readonly IPAddress s_multicastAddress = IPAddress.Parse("239.192.0.4");
    private static readonly int s_multicastPort = 9192;

    private readonly SocketWrapper _socketWrapper = new SocketWrapper(s_multicastAddress, s_multicastPort);
    private readonly OngoingGamesList _ongoingGamesList;
    private MainWindow _mainWindow;

    private IGameInfo.IGameConfig? _gameConfig; 

    private Timer? _ongoingTimer;
    
    private int MyPlayerId;

    public Presenter()
    {
        _ongoingGamesList = new OngoingGamesList(_socketWrapper);
        _ongoingGamesList.Update += OnGameListUpdated;
        _ongoingGamesList.Start();
        _socketWrapper.StartListeningIncomingMessages();
    }

    public void AttachMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void StartNewGame(GameCreationWindow.GameConfig gameConfig)
    {
        Log.Debug("Starting new game as MASTER.");
        _gameConfig = gameConfig;
        _ongoingGamesList.Stop();
        ongoingGame = new Model.Game.Core.Game(gameConfig.GameName, gameConfig.StateDelay_ms, gameConfig.FieldWidth, 
            gameConfig.FieldHeight, gameConfig.FoodStatic);
        MyPlayerId = ongoingGame.NewPlayer(gameConfig.PlayerName);
        _nodeContext = new NodeContext(MyPlayerId, null, this, ongoingGame, _socketWrapper);
        _nodeContext.GameStateUpdated += (o, args) => GameStateUpdated?.Invoke(o, args);
        Master master = Master.InitNew(_nodeContext);
        _nodeContext.CurrentState = master;
        Log.Debug("Successfully started new game as MASTER.");
    }

    public void ChangePlayerDirection(int playerId, ISnakeState.Direction direction)
    {
        _nodeContext?.CurrentState?.ChangePlayerDirection(playerId, direction);
    }

    public void ChangeMyPlayerDirection(ISnakeState.Direction direction)
    {
        ChangePlayerDirection(MyPlayerId, direction);
    }

    public bool IsGameGoing()
    {
        return ongoingGame != null;
    }

    internal void ExitGame()
    {
        Log.Debug("Exiting ongoing game.");
        if (ongoingGame == null)
        {
            Log.Error("No game is going.");
            throw new InvalidOperationException("No game is going.");
        }
        _nodeContext?.Dispose();
        _ongoingTimer?.Dispose();
        _ongoingTimer = null;
        ongoingGame = null;
        _ongoingGamesList.Start();
        Log.Debug("Game is closed.");
    }

    public void ExitApplication()
    {
        _socketWrapper.Dispose();
    } 

    internal void OnError(string errorMessage)
    {
        _mainWindow.DisplayError(errorMessage);
    }

    private void OnGameListUpdated(List<Tuple<IPEndPoint, GameMessage>> infos)
    {
        List<IOngoingGameInfo> ongoingGameInfos = [];
        foreach (var info in infos)
        {
            ongoingGameInfos.Add(ToOngoingGameInfo(info));
        }
        GameListUpdated?.Invoke(ongoingGameInfos);
    }

    private IOngoingGameInfo ToOngoingGameInfo(Tuple<IPEndPoint, GameMessage> info)
    {
        GameAnnouncement gameAnnouncement = info.Item2.Announcement.Games[0];
        return new OngoingGameInfo(gameAnnouncement.GameName, ConnectedNode.ParsePlayerStates(gameAnnouncement.Players),
            new OngoingGamesList.GameConfig(gameAnnouncement.Config), gameAnnouncement.CanJoin, info.Item1, gameAnnouncement);
    }

    internal void JoinGame(IOngoingGameInfo selectedGame, string playerName)
    {
        Log.Debug($"Trying to join to {((OngoingGameInfo)selectedGame).SenderAddress} as NORMAL");
        if (IsGameGoing())
        {
            Log.Debug("The other game is already going. Exiting it and proceeding to join.");
            ExitGame();
        }
        OngoingGameInfo gameInfo = selectedGame as OngoingGameInfo;
        _gameConfig = gameInfo.GameConfig;
        _ongoingGamesList.Stop();
        ongoingGame = new Model.Game.Core.Game(gameInfo.Name, _gameConfig.StateDelay_ms, _gameConfig.FieldWidth, _gameConfig.FieldHeight, _gameConfig.FoodStatic);
        _nodeContext = new NodeContext(-1, null, this, ongoingGame, _socketWrapper);
        _nodeContext.GameStateUpdated += (o, args) =>
        {
            GameStateUpdated?.Invoke(o, args);
        };
        GamePlayer masterState = null;
        foreach (GamePlayer player in gameInfo.GameAnnouncement.Players.Players)
        {
            if (player.Role != NodeRole.Master)
            {
                continue;
            }
            masterState = player;
            break;
        }
        Log.Debug($"Master of the game is: {masterState.Id}");
        ConnectingNode connecting = ConnectingNode.JoinAsNormal(_nodeContext, masterState, gameInfo.SenderAddress, gameInfo.Name, playerName);
        _nodeContext.CurrentState = connecting;
    }

    internal void OnJoinError(GameMessage.Types.ErrorMsg error)
    {
        Log.Debug("Couldn't join the game.");
        _mainWindow.DisplayError(error.ErrorMessage);
        ExitGame();
    }

    internal void OnJoinSuccess(int id)
    {
        Debug.Assert(_gameConfig != null);
        MyPlayerId = id;
        _mainWindow.OnGameJoined(_gameConfig);   
    }
}
