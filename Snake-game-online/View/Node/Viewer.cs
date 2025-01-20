using Serilog;
using Snakes;
using System.Net;

namespace Network.Node;

internal class Viewer : ConnectedNode
{
    public Viewer(NodeContext context) : base(context)
    {
        Log.Debug("Become viewer.");
    }

    public override void OnNodeDisconnect(int nodeId)
    {
        bool didMasterDisconnect = _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode().Id == nodeId);
        base.OnNodeDisconnect(nodeId);
        if (!didMasterDisconnect)
        {
            return;
        }
        _context.SynchronizedOnNodes(_ =>
        {
            GamePlayer? deputy = _context.Nodes.GetDeputyNode();
            if (deputy == null)
            {
                throw new Exception("Invalid state(should cancel the game).");
            }
            _context.Nodes.Remove(deputy);
            _context.Nodes.Add(new GamePlayer(deputy)
            {
                Role = NodeRole.Master,
            });
        });
    }

    public override void OnRoleChangeReceived(IPEndPoint sender, GameMessage message)
    {
        if (!message.RoleChange.HasSenderRole)
        {
            return;
        }
        if (message.RoleChange.SenderRole != NodeRole.Master || message.SenderId != _context.SynchronizedOnNodes(_ => _context.Nodes.GetDeputyNode().Id))
        {
            return;
        }
        _context.SynchronizedOnNodes(_ =>
        {
            GamePlayer deputy = _context.Nodes.GetDeputyNode();
            _context.Nodes.Remove(deputy.Id);
            _context.Nodes.Add(new GamePlayer(deputy)
            {
                Role = NodeRole.Master,
            });
        });
    }
}
