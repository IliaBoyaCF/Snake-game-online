using Serilog;
using Snake_game_online.model.Game.GameState;
using SnakeGameOnline.Model.Game.Core;
using Snakes;
using System.Net;
using static SnakeGameOnline.Presenter;

namespace SnakeGameOnline.Model.Network.Node;

internal class NodeContext : IDisposable
{
    private readonly SocketWrapper _socketWrapper;
    public NodeStorage Nodes { get; set; }

    public int MyId { get; set; }

    public IGame Game { get; set; }

    public Presenter Presenter { get; set; }

    public NodeRadar NodeRadar { get; set; }

    private object _gameLock = new object();

    private object _nodesLock = new object();

    private long _seq = 0;

    public long MessageSeq 
    { 
        get
        {
            return _seq++;
        }
    }

    public MessageDeliveryController MessageDeliveryController { get; set; }

    private Node? _node;

    private Timer? _masterRoutineTimer;
    private Timer? _masterAnnounceTimer;
    private const int s_announcementDelay = 1_000;

    public event OnGameStateUpdate? GameStateUpdated;

    public Node? CurrentState 
    {  
        get
        {
            return _node;
        }
        set
        {
            if (_node is Master)
            {
                _masterAnnounceTimer?.Dispose();
                _masterRoutineTimer?.Dispose();
            }
            _node = value;
            if (value is Master)
            {
                _masterRoutineTimer = new Timer((node) =>
                {
                    Master? master = (Master?)node;
                    master?.Routine();
                }, _node, 0, Game.StateDelayMs);
                _masterAnnounceTimer = new Timer((node) =>
                {
                    Master? master = (Master?)node;
                    master?.Announce();
                }, _node, 0, s_announcementDelay);
            }

        }
    }

    private void SynchronizedAction(Action<NodeContext> action, object lockVal)
    {
        lock (lockVal)
        {
            action.Invoke(this);
        }
    }

    private T SynchronizedFunction<T>(Func<NodeContext, T> func, object lockVal)
    {
        lock (lockVal)
        {
            return func.Invoke(this);
        }
    }

    public void SynchronizedOnGame(Action<NodeContext> action)
    {
        SynchronizedAction(action, _gameLock);
    }

    public T SynchronizedOnGame<T>(Func<NodeContext, T> func)
    {
        return SynchronizedFunction(func, _gameLock);
    }

    public void SynchronizedOnNodes(Action<NodeContext> action)
    {
        SynchronizedAction(action, _nodesLock);
    }

    public T SynchronizedOnNodes<T>(Func<NodeContext, T> func)
    {
        return SynchronizedFunction(func, _nodesLock);
    }

    public NodeContext(int myId, Node? node, Presenter presenter, IGame game, SocketWrapper socketWrapper)
    {
        _node = node;
        _socketWrapper = socketWrapper;
        _socketWrapper.OnMulticastMessageReceived += OnMulticastReceivedHandler;
        _socketWrapper.OnUnicastMessageReceived += OnUnicastReceivedHandler;
        MyId = myId;
        Presenter = presenter;
        Game = game;
        NodeRadar = new NodeRadar(this, game.StateDelayMs, myId);
        NodeRadar.NodeDisconnected += OnNodeDisconnected;
        Nodes = new NodeStorage();
        MessageDeliveryController = new MessageDeliveryController(socketWrapper, game.StateDelayMs, (int)(game.StateDelayMs * 0.2));
    }

    public void OnMulticastReceivedHandler(IPEndPoint sender, GameMessage message)
    {
        if (_node == null)
        {
            return;
        }
        switch (message.TypeCase)
        {
            case GameMessage.TypeOneofCase.Discover:
                {
                    _node.OnDiscoverReceived(sender, message);
                    return;
                }
            default:
                {
                    return;
                }
        }
    }

    public void OnUnicastReceivedHandler(IPEndPoint sender, GameMessage message)
    {
        if (_node == null) return;
        if (message.HasSenderId)
        {
            NodeRadar.GotMessageFrom(message.SenderId);
        }
        switch (message.TypeCase)
        {
            case GameMessage.TypeOneofCase.Ping:
                {
                    _node.OnPingReceived(sender, message); break;
                }
            case GameMessage.TypeOneofCase.Steer:
                {
                    _node.OnSteerReceived(sender, message); break;
                }
            case GameMessage.TypeOneofCase.Ack:
                {
                    _node.OnAckReceived(sender, message); break;
                }
            case GameMessage.TypeOneofCase.State:
                {
                    _node.OnStateReceived(sender, message); break;
                }
            case GameMessage.TypeOneofCase.Join:
                {
                    _node.OnJoinReceived(sender, message); break;
                }
            case GameMessage.TypeOneofCase.Error:
                {
                    _node.OnErrorReceived(sender, message); break;
                }
            case GameMessage.TypeOneofCase.RoleChange:
                {
                    _node.OnRoleChangeReceived(sender, message); break;
                }
            default:
                break;
        }
    }

    public void OnGameStateUpdate(IGameState gameState)
    {
        Log.Debug("Calling on game state update delegate.");
        GameStateUpdated?.Invoke(this, new GameStateUpdatedEventArgs(gameState));
    }

    public void OnNodeDisconnected(int nodeId)
    {
        CurrentState?.OnNodeDisconnect(nodeId);
    }

    public void Dispose()
    {
        _masterAnnounceTimer?.Dispose();
        _masterRoutineTimer?.Dispose();
        MessageDeliveryController.Dispose();
        NodeRadar.Stop();
        _socketWrapper.OnUnicastMessageReceived -= OnUnicastReceivedHandler;
        _socketWrapper.OnMulticastMessageReceived -= OnMulticastReceivedHandler;
    }
}
