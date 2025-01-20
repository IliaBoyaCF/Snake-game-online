using Serilog;
using Snakes;
using System.Net;

namespace Network.Node;

internal class Normal : ConnectedNode
{
    public Normal(NodeContext context) : base(context)
    {
        Log.Debug("Become Normal.");
    }

    public override void OnRoleChangeReceived(IPEndPoint sender, GameMessage message)
    {
        GamePlayer master = _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode());
        IPEndPoint masterIPEndPoint = GetIpEndPoint(master);
        if (message.RoleChange.HasReceiverRole && (master.Id != message.SenderId || !masterIPEndPoint.Equals(sender)))
        {
            SendWarningToCheater(sender);
            return;
        }
        if (!_context.Nodes.Contains((n) => n.Id == message.SenderId))
        {
            return;
        }
        if (message.RoleChange.HasSenderRole)
        {
            if (message.RoleChange.SenderRole == NodeRole.Master && message.SenderId != _context.SynchronizedOnNodes(_ => _context.Nodes.GetDeputyNode().Id))
            {
                _context.SynchronizedOnNodes(_ =>
                {
                    GamePlayer deputy = _context.Nodes.GetDeputyNode();
                    _context.Nodes.Remove(deputy.Id);
                    _context.Nodes.Add(new GamePlayer(deputy)
                    {
                        Role = NodeRole.Master,
                    });
                });
                return;
            }
            if (message.RoleChange.SenderRole != NodeRole.Master || master != null)
            {
                return;
            }
            GamePlayer? gamePlayer = _context.Nodes.FindById(message.SenderId);
            if (gamePlayer == null)
            {
                return;
            }
            _context.SynchronizedOnNodes(_ =>
            {
                _context.Nodes.Remove(gamePlayer);
                _context.Nodes.Add(new GamePlayer(gamePlayer)
                {
                    Role = NodeRole.Master,
                });
            });
        }
        switch (message.RoleChange.ReceiverRole)
        {
            case NodeRole.Viewer:
                {
                    _context.SynchronizedOnNodes(_ =>
                    {
                        GamePlayer me = _context.Nodes.FindById(_context.MyId);
                        _context.Nodes.Remove(me);
                        _context.Nodes.Add(new GamePlayer()
                        {
                            Role = NodeRole.Viewer
                        });
                    });
                    _context.CurrentState = new Viewer(_context);
                    break;
                }
            case NodeRole.Deputy:
                {
                    _context.SynchronizedOnNodes(_ =>
                    {
                        GamePlayer me = _context.Nodes.FindById(_context.MyId);
                        _context.Nodes.Remove(me);
                        _context.Nodes.Add(new GamePlayer(me)
                        {
                            Role = NodeRole.Deputy
                        });
                    });
                    _context.CurrentState = new Deputy(_context);
                    break;
                }
            default:
                {
                    return;
                }
        }
    }

    public override void OnNodeDisconnect(int nodeId)
    {
        bool didMasterDisconnect = _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode().Id == nodeId);
        base.OnNodeDisconnect(nodeId);
        if (!didMasterDisconnect)
        {
            return;
        }
        GamePlayer? deputy = _context.SynchronizedOnNodes(_ => _context.Nodes.GetDeputyNode());
        if (deputy == null)
        {
            throw new Exception("Invalid state(should cancel the game).");
        }
        _context.SynchronizedOnNodes(_ =>
        {
            _context.Nodes.Remove(deputy);
            _context.Nodes.Add(new GamePlayer(deputy)
            {
                Role = NodeRole.Master,
            });
        });
    }
}
