namespace SnakeGameOnline.Model.Network.Node;

internal class NodeRadar
{
    
    private readonly int s_period;

    public delegate void OnNodeDisconnected(int nodeId);

    public event OnNodeDisconnected? NodeDisconnected;

    private readonly NodeContext _nodeContext;

    private readonly Dictionary<int, Tuple<long, long>> _lastTimeGotMessageFrom = [];

    private readonly object _lock = new object();

    private readonly int _shouldPingDelay;

    private readonly int _disconnectedDelay;

    private readonly int _doNotPingId;

    private Timer? _timer;

    public NodeRadar(NodeContext nodeContext, int stateDelay, int doNotPingId)
    {
        _doNotPingId = doNotPingId;
        _nodeContext = nodeContext;
        _shouldPingDelay = stateDelay / 10;
        _disconnectedDelay = (int)double.Floor(stateDelay * 0.8);
        s_period = _shouldPingDelay;
    }

    public void Start()
    {
        lock (_lock)
        {
            _lastTimeGotMessageFrom.Clear();
            _timer = new Timer((_) => Routine(), null, 0, s_period);
        }
    }

    private void Routine()
    {
        List<int> disconnected = [];
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            foreach (KeyValuePair<int, Tuple<long, long>> id_lastTimeGotMessagePair in _lastTimeGotMessageFrom.ToList())
            {
                if (id_lastTimeGotMessagePair.Key == _doNotPingId)
                {
                    continue;
                }
                if (currentTime - id_lastTimeGotMessagePair.Value.Item1 > _disconnectedDelay)
                {
                    disconnected.Add(id_lastTimeGotMessagePair.Key);
                    continue;
                }
                if (currentTime - id_lastTimeGotMessagePair.Value.Item2 > _shouldPingDelay)
                {
                    _nodeContext.CurrentState.SendPingTo(id_lastTimeGotMessagePair.Key);
                }
            }
            foreach (int disconnectedId in disconnected)
            {
                _lastTimeGotMessageFrom.Remove(disconnectedId);
            }
        }
        foreach (int disconnectedId in disconnected)
        {
            NodeDisconnected?.Invoke(disconnectedId);
        }
    }

    public void GotMessageFrom(int id)
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            Tuple<long, long> newVal;
            if (_lastTimeGotMessageFrom.ContainsKey(id))
            {
                newVal = _lastTimeGotMessageFrom[id];
                _lastTimeGotMessageFrom.Remove(id);
                newVal = Tuple.Create(currentTime, newVal.Item2);
            }
            else
            {
                newVal = Tuple.Create(currentTime, currentTime);
            }
            _lastTimeGotMessageFrom.Add(id, newVal);
        }
    }

    public void SentMessageTo(int id)
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            Tuple<long, long> newVal;
            if (_lastTimeGotMessageFrom.ContainsKey(id))
            {
                newVal = _lastTimeGotMessageFrom[id];
                _lastTimeGotMessageFrom.Remove(id);
                newVal = Tuple.Create(newVal.Item1, currentTime);
            }
            else
            {
                newVal = Tuple.Create(currentTime, currentTime);
            }
            _lastTimeGotMessageFrom.Add(id, newVal);
        }
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
