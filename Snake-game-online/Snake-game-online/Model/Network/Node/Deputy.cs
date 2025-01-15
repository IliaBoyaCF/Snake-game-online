using Serilog;
using Snakes;
using System.Net;

namespace SnakeGameOnline.Model.Network.Node;

internal class Deputy : ConnectedNode
{
    public Deputy(NodeContext context) : base(context)
    {
        Log.Debug("Become deputy.");
    }

    public override void OnNodeDisconnect(int nodeId)
    {
        bool didMasterDisconnect = _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode()).Id == nodeId;
        if (didMasterDisconnect)
        {
            _context.CurrentState = Master.InitFromDeputyPromotion(_context, nodeId);
            return;
        }
        base.OnNodeDisconnect(nodeId);
    }

    public override void OnRoleChangeReceived(IPEndPoint sender, GameMessage message)
    {
        _context.SynchronizedOnNodes(node =>
        {
            if (AnswerIsDelivering(sender, message))
            {
                return;
            }
            if (message.RoleChange.HasReceiverRole && _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode().Id != message.SenderId || GetIpEndPoint(_context.Nodes.GetMasterNode()) != sender))
            {
                SendWarningToCheater(sender);
                return;
            }
            if (!_context.SynchronizedOnNodes(_ => _context.Nodes.Contains((n) => n.Id == message.SenderId)))
            {
                return;
            }
            if (message.RoleChange.HasSenderRole)
            {
                if (message.RoleChange.SenderRole != NodeRole.Master || _context.Nodes.GetMasterNode() != null)
                {
                    return;
                }
                GamePlayer? gamePlayer = _context.Nodes.FindById(message.SenderId);
                if (gamePlayer == null)
                {
                    return;
                }
                _context.Nodes.Remove(gamePlayer);
                _context.Nodes.Add(new GamePlayer(gamePlayer)
                {
                    Role = NodeRole.Master,
                });
            }
            if (message.RoleChange.ReceiverRole != NodeRole.Viewer)
            {
                return;
            }
            GamePlayer me = _context.Nodes.FindById(_context.MyId);
            _context.Nodes.Remove(me);
            _context.Nodes.Add(new GamePlayer()
            {
                Role = NodeRole.Viewer
            });
            _context.CurrentState = new Viewer(_context);
        });
    }


}
