using Snakes;
using System.Collections;

namespace Network.Node;

public class NodeStorage : IEnumerable<GamePlayer>
{

    private readonly List<GamePlayer> _viewerNodes = [];

    private readonly List<GamePlayer> _playerNodes = [];

    private GamePlayer? _masterNode;

    private GamePlayer? _deputyNode;

    private readonly object _lock = new object();

    public void Add(GamePlayer node)
    {
        lock (_lock)
        {
            switch (node.Role)
            {
                case NodeRole.Viewer:
                    if (_viewerNodes.Contains(node))
                    {
                        throw new InvalidOperationException("Can't have two same nodes.");
                    }
                    _viewerNodes.Add(node);
                    break;
                case NodeRole.Master:
                    if (_masterNode != null)
                    {
                        throw new InvalidOperationException("Can't have two master nodes.");
                    }
                    _masterNode = node;
                    _playerNodes.Add(node);
                    break;
                case NodeRole.Deputy:
                    if (_deputyNode != null)
                    {
                        throw new InvalidOperationException("Can't have two deputy nodes.");
                    }
                    _deputyNode = node;
                    _playerNodes.Add(node);
                    break;
                case NodeRole.Normal:
                    if (_playerNodes.Contains(node))
                    {
                        throw new InvalidOperationException("Can't have two same nodes.");
                    }
                    _playerNodes.Add(node);
                    break;
                default:
                    throw new ArgumentException("Unknown node role");
            }
        }
    }

    public void Remove(int Id)
    {
        Remove(FindById(Id));
    }

    public void Remove(GamePlayer node)
    {
        lock (_lock)
        {
            switch (node.Role)
            {
                case NodeRole.Viewer:
                    _viewerNodes.Remove(node);
                    break;
                case NodeRole.Master:
                    _masterNode = null;
                    int a = _playerNodes.IndexOf(node);
                    if (a == -1)
                    {
                        break;
                    }
                    int b = _playerNodes.Count;
                    _playerNodes.RemoveAt(a);
                    //_playerNodes.Remove(node);
                    break;
                case NodeRole.Deputy:
                    _deputyNode = null;
                    _playerNodes.Remove(node);
                    break;
                case NodeRole.Normal:
                    _playerNodes.Remove(node);
                    break;
                default:
                    throw new ArgumentException("Unknown node role");
            }
        }
    }

    public GamePlayer? GetMasterNode()
    {
        return _masterNode;
    }

    public GamePlayer? GetDeputyNode()
    {
        return _deputyNode;
    }

    public bool Contains(Predicate<GamePlayer> predicate)
    {
        bool contains;
        lock (_lock)
        {
            contains = _viewerNodes.Find(predicate) != null || _playerNodes.Find(predicate) != null;
        }
        return contains;
    }

    public bool Contains(int id)
    {
        bool contains;
        lock (_lock)
        {
            contains = _viewerNodes.Find(n => n.Id == id) != null || _playerNodes.Find(n => n.Id == id) != null;
        }
        return contains;
    }

    public GamePlayer Find(Predicate<GamePlayer> predicate)
    {
        GamePlayer? player = null;
        lock (_lock)
        {
            player = _viewerNodes.Find(predicate);
            if (player == null)
            {
                player = _playerNodes.Find(predicate);
            }
        }
        if (player == null)
        {
            throw new ArgumentException("Node doesn't exists");
        }
        return player;
    }

    public IEnumerator<GamePlayer> GetEnumerator()
    {
        List<GamePlayer> res;
        lock (_lock)
        {
            res = new List<GamePlayer>(_playerNodes);
            res.AddRange(_viewerNodes);
        }
        return res.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public List<int> FindPlayers()
    {
        List<int> res;
        lock (_lock)
        {
            res = _playerNodes.Select(n => n.Id).ToList();
        }
        return res;
    }

    public GamePlayer FindById(int id)
    {
        GamePlayer res;
        lock (_lock)
        {
            res = _playerNodes.Find(n => n.Id == id);
        }
        return res;
    }
}
