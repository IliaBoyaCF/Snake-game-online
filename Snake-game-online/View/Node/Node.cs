using SnakeOnline.Game.States;
using Snakes;
using System.Net;

namespace Network.Node;

public abstract class Node : IDisposable
{
    protected readonly NodeContext _context;

    public Node(NodeContext context)
    {
        _context = context;
        _context.CurrentState = this;
    }

    protected ISnakeState.Direction GetDirection(Direction direction)
    {
        return direction switch
        {
            Direction.Up => ISnakeState.Direction.UP,
            Direction.Left => ISnakeState.Direction.LEFT,
            Direction.Down => ISnakeState.Direction.DOWN,
            Direction.Right => ISnakeState.Direction.RIGHT,
            _ => throw new ArgumentException("Unknown direction."),
        };
    }

    protected void SendAckTo(IPEndPoint sender, GameMessage message, int targetId)
    {
        _context.MessageDeliveryController.SendUnicastOnce(new GameMessage()
        {
            MsgSeq = message.MsgSeq,
            SenderId = _context.MyId,
            ReceiverId = targetId,
            Ack = new GameMessage.Types.AckMsg(),
        }, sender);
        _context.NodeRadar.SentMessageTo(targetId);
    }

    protected static IPEndPoint GetIpEndPoint(GamePlayer master)
    {
        return new IPEndPoint(IPAddress.Parse(master.IpAddress), master.Port);
    }

    protected Direction ToProtobufDirection(ISnakeState.Direction direction)
    {
        return direction switch
        {
            ISnakeState.Direction.UP => Direction.Up,
            ISnakeState.Direction.LEFT => Direction.Left,
            ISnakeState.Direction.DOWN => Direction.Down,
            ISnakeState.Direction.RIGHT => Direction.Right,
            _ => throw new ArgumentException("Unknown direction."),
        };
    }

    public abstract void ChangePlayerDirection(int id, ISnakeState.Direction direction);

    public virtual void SendPingTo(int playerId)
    {
        GamePlayer gamePlayer = null;
        bool nodeExists = _context.SynchronizedOnNodes(_ =>
        {
            if (_context.Nodes.Contains(playerId))
            {
                gamePlayer = _context.Nodes.Find((n) => n.Id == playerId);
                return true;
            }
            return false;
        });

        if (!nodeExists)
        {
            return;
        }

        _context.MessageDeliveryController.Deliver(new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            SenderId = _context.MyId,
            ReceiverId = playerId,
            Ping = new GameMessage.Types.PingMsg() { },
        }, new IPEndPoint(IPAddress.Parse(gamePlayer.IpAddress), gamePlayer.Port));
        _context.NodeRadar.SentMessageTo(playerId);
    }

    protected bool AnswerIsDelivering(IPEndPoint sender, GameMessage message)
    {
        return _context.MessageDeliveryController.GetPending(sender).Find((m) => m.MsgSeq == message.MsgSeq) != null;
    }

    protected void SendWarningToCheater(IPEndPoint cheaterAddress)
    {
        _context.MessageDeliveryController.Deliver(new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            SenderId = _context.MyId,
            Error = new GameMessage.Types.ErrorMsg()
            {
                ErrorMessage = "Cheating is bad!!!",
            }
        }, cheaterAddress);
    }

    public virtual void OnPingReceived(IPEndPoint sender, GameMessage message)
    {

        SendAckTo(sender, message, message.SenderId);
        _context.NodeRadar.GotMessageFrom(message.SenderId);
    }

    public abstract void OnSteerReceived(IPEndPoint sender, GameMessage message);

    // Confirms delivery of given message.
    public virtual void OnAckReceived(IPEndPoint sender, GameMessage message)
    {
        _context.NodeRadar.GotMessageFrom(message.SenderId);
        _context.MessageDeliveryController.ConfirmDelivery(message.MsgSeq);
    }

    public abstract void OnStateReceived(IPEndPoint sender, GameMessage message);

    public virtual void OnAnnouncementReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public abstract void OnJoinReceived(IPEndPoint sender, GameMessage message);

    // Tells presenter to display error message on View.
    public virtual void OnErrorReceived(IPEndPoint sender, GameMessage message)
    {
        _context.NodeRadar.GotMessageFrom(message.SenderId);
        _context.Presenter.OnError(message.Error.ErrorMessage);
    }

    public abstract void OnRoleChangeReceived(IPEndPoint sender, GameMessage message);

    public abstract void OnDiscoverReceived(IPEndPoint sender, GameMessage message);

    // Removes all incoming not delivered messages to node from queue.
    public virtual void OnNodeDisconnect(int nodeId)
    {
        _context.SynchronizedOnNodes(_ =>
        {
            if (!_context.Nodes.Contains(nodeId))
            {
                return;
            }
            GamePlayer node = _context.Nodes.Find((n) => n.Id == nodeId);
            _context.MessageDeliveryController.RemoveAllDeliveries(new IPEndPoint(IPAddress.Parse(node.IpAddress), node.Port));
            _context.Nodes.Remove(node);
        });
    }

    public virtual void Dispose()
    {
        _context.NodeRadar.Stop();
    }
}
