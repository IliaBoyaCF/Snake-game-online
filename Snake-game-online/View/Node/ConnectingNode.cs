using Serilog;
using SnakeOnline.Game.States;
using Snakes;
using System.Net;

namespace Network.Node;

public class ConnectingNode : Node
{
    private string _playerName;
    private NodeRole _role;

    public static ConnectingNode JoinAsViewer(NodeContext context, GamePlayer masterState, IPEndPoint masterAddress, string gameName, string playerName)
    {
        return Join(context, masterState, masterAddress, gameName, playerName, NodeRole.Viewer);
    }

    // Need to add master node as an argument
    public static ConnectingNode JoinAsNormal(NodeContext context, GamePlayer masterState, IPEndPoint masterAddress, string gameName, string playerName)
    {
        return Join(context, masterState, masterAddress, gameName, playerName, NodeRole.Normal);
    }

    private static ConnectingNode Join(NodeContext context, GamePlayer masterState, IPEndPoint masterAddress, string gameName, string playerName, NodeRole role)
    {
        ConnectingNode connectingNode = new ConnectingNode(context);
        connectingNode._playerName = playerName;
        connectingNode._role = role;
        connectingNode._context.Nodes.Add(new GamePlayer(masterState)
        {
            IpAddress = masterAddress.Address.ToString(),
            Port = masterAddress.Port,
        });
        connectingNode._context.MessageDeliveryController.Deliver(new GameMessage()
        {
            MsgSeq = connectingNode._context.MessageSeq,
            SenderId = masterState.Id,
            Join = new GameMessage.Types.JoinMsg()
            {
                RequestedRole = role,
                PlayerType = PlayerType.Human,
                GameName = gameName,
                PlayerName = playerName,
            }
        }, masterAddress);
        Log.Debug($"Created ConnectingNode and sent join message to {masterAddress}.");
        return connectingNode;
    }

    public ConnectingNode(NodeContext context) : base(context)
    {
    }

    public override void ChangePlayerDirection(int id, ISnakeState.Direction direction)
    {
        return;
    }

    public override void OnDiscoverReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnJoinReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnRoleChangeReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnStateReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnSteerReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnPingReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnAckReceived(IPEndPoint sender, GameMessage message)
    {
        if (message.MsgSeq != 0 && _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode().Id != message.SenderId))
        {
            return;
        }
        _context.MyId = message.ReceiverId;
        Log.Debug($"Connected to game. My in game id is {_context.MyId}");
        switch (_role)
        {
            case NodeRole.Normal:
                {
                    _context.SynchronizedOnNodes((_) => _context.Nodes.Add(new GamePlayer()
                    {
                        Id = _context.MyId,
                        Name = _playerName,
                        Score = 0,
                        IpAddress = IPAddress.Any.ToString(),
                        Port = 0,
                        Role = NodeRole.Normal,
                        Type = PlayerType.Human,
                    }));
                    _context.CurrentState = new Normal(_context);
                    break;
                }
            case NodeRole.Viewer:
                {
                    _context.SynchronizedOnNodes((_) => _context.Nodes.Add(new GamePlayer()
                    {
                        Id = _context.MyId,
                        Name = _playerName,
                        Score = 0,
                        IpAddress = IPAddress.Any.ToString(),
                        Port = 0,
                        Role = NodeRole.Viewer,
                        Type = PlayerType.Human,
                    }));
                    _context.CurrentState = new Viewer(_context);
                    break;
                }
            default:
                {
                    throw new ArgumentException("Can't connect other than as Viewer or Normal.");
                }
        }
        _context.NodeRadar.GotMessageFrom(message.SenderId);
        _context.MessageDeliveryController.ConfirmDelivery(message.MsgSeq);
        _context.Presenter.OnJoinSuccess(_context.MyId);
    }

    public override void OnErrorReceived(IPEndPoint sender, GameMessage message)
    {
        _context.Presenter.OnJoinError(message.Error);
    }
}
