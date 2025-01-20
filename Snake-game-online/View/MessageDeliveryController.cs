using System.Net;
using Snakes;

namespace Network;

public class MessageDeliveryController : IDisposable
{

    private readonly SocketWrapper _socketWrapper;

    private readonly object _lock = new object();

    private readonly int _updateDelay;
    private readonly int _messageResendDelay;

    private readonly Dictionary<long, PendingMessage> _pendingMessages = [];

    private class PendingMessage
    {

        public GameMessage Message { get; }
        public IPEndPoint Destination { get; }
        public long TimeSent { get; set; }

        public PendingMessage(GameMessage message, IPEndPoint destination, long timeSent)
        {
            Message = message;
            Destination = destination;
            TimeSent = timeSent;
        }
    }

    private Timer? _timer;

    public MessageDeliveryController(SocketWrapper socketWrapper, int updateDelay, int messageResendDelay)
    {
        _socketWrapper = socketWrapper;
        _updateDelay = updateDelay;
        _messageResendDelay = messageResendDelay;
    }

    public void SendMulticastOnce(GameMessage message)
    {
        _socketWrapper.SendMulticast(message);
    }

    public void SendUnicastOnce(GameMessage message, IPEndPoint destination)
    {
        _socketWrapper.SendUnicast(message, destination);
    }

    public void Start()
    {
        _timer = new Timer(Update, this, 0, _updateDelay);
    }

    private void Update(object? state)
    {
        List<PendingMessage> needResend = [];
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            foreach (PendingMessage pendingMessage in _pendingMessages.Values)
            {
                if (currentTime - pendingMessage.TimeSent <= _messageResendDelay)
                {
                    continue;
                }
                pendingMessage.TimeSent = currentTime;
                needResend.Add(pendingMessage);
            }
        }
        foreach (PendingMessage pendingMessage in needResend)
        {
            _socketWrapper.SendUnicast(pendingMessage.Message, pendingMessage.Destination);
        }
    }

    public void Deliver(GameMessage message, IPEndPoint destination)
    {
        lock (_lock)
        {
            if (_pendingMessages.ContainsKey(message.MsgSeq))
            {
                return;
            }
            _pendingMessages.Add(message.MsgSeq, new PendingMessage(message, destination, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }
        SendUnicastOnce(message, destination);
    }

    public bool IsPending(GameMessage.TypeOneofCase type, IPEndPoint destination)
    {
        PendingMessage? res;
        lock (_lock)
        {
            res = _pendingMessages.Values.ToList().Find((m) =>
            {
                return m.Message.TypeCase == type && m.Destination == destination;
            });
        }
        return res != null;
    }

    public void ResendAllMessagesFromOldToNew(IPEndPoint old, IPEndPoint @new, int newReceiverId)
    {
        List<PendingMessage> pendingMessages = GetPendingMessages(old);
        lock (_lock)
        {
            foreach (PendingMessage pendingMessage in _pendingMessages.Values)
            {
                _pendingMessages.Remove(pendingMessage.Message.MsgSeq);
                _pendingMessages.Add(pendingMessage.Message.MsgSeq, new PendingMessage(new GameMessage(pendingMessage.Message)
                {
                    ReceiverId = newReceiverId,
                }, @new, pendingMessage.TimeSent));
            }
        }
    }


    public List<GameMessage> GetPending(IPEndPoint destination)
    {
        return GetPendingMessages(destination).Select((pm) => pm.Message).ToList();
    }

    private List<PendingMessage> GetPendingMessages(IPEndPoint destination)
    {
        List<PendingMessage> res = [];
        lock (_lock)
        {
            res = _pendingMessages.Values.ToList().FindAll((m) =>
            {
                return m.Destination == destination;
            });
        }
        return res;
    }

    public void RemoveAllDeliveries(IPEndPoint destination)
    {
        List<PendingMessage> pendingMessages = GetPendingMessages(destination);
        lock (_lock)
        {
            foreach (PendingMessage message in pendingMessages)
            {
                _pendingMessages.Remove(message.Message.MsgSeq);
            }
        }
    }

    public void ConfirmDelivery(long seq)
    {
        lock (_lock)
        {
            _pendingMessages.Remove(seq);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    internal bool IsPending(Predicate<GameMessage> predicate)
    {
        lock (_lock)
        {
            foreach (PendingMessage message in _pendingMessages.Values)
            {
                if (predicate.Invoke(message.Message))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
