using Google.Protobuf;
using Serilog;
using Snakes;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SnakeGameOnline.Model.Network;

public class SocketWrapper : IDisposable
{

    public delegate void OnUnicastMessageReceivedHandler(IPEndPoint sender, GameMessage message);

    public delegate void OnMulticastMessageReceivedHander(IPEndPoint sender, GameMessage message);

    public event OnUnicastMessageReceivedHandler? OnUnicastMessageReceived;
    public event OnMulticastMessageReceivedHander? OnMulticastMessageReceived;

    private readonly IPEndPoint _multicastAddress;

    private readonly Socket _unicastSocket;
    private readonly Socket _multicastSocket;

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly CancellationToken _cancellationToken;

    private Thread? _listeningThread;

    public void StartListeningIncomingMessages()
    {
        Log.Debug("Wrapper started to listen incoming messages.");
        _listeningThread = new Thread(Listen);
        _listeningThread.Start();
    }

    private void Listen()
    {
        while (true)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                break;
            }
            try
            {
                if (_multicastSocket.Available > 0)
                {
                    byte[] bytes = new byte[_multicastSocket.Available];
                    EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                    _multicastSocket.ReceiveFrom(bytes, ref sender);
                    GameMessage message = GameMessage.Parser.ParseFrom(bytes);
                    Log.Debug($"Wrapper got {message.TypeCase} message from {sender} via multicast.");
                    OnMulticastMessageReceived?.Invoke((IPEndPoint)sender, message);
                }
            }
            catch (Exception e) when (e is SocketException || e is InvalidProtocolBufferException) { }
            try
            {
                if (_unicastSocket.Available > 0)
                {
                    byte[] bytes = new byte[_unicastSocket.Available];
                    EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    _unicastSocket.ReceiveFrom(bytes, ref sender);
                    GameMessage message = GameMessage.Parser.ParseFrom(bytes);
                    Log.Debug($"Wrapper got {message.TypeCase} message from {sender} via unicast.");
                    OnUnicastMessageReceived?.Invoke((IPEndPoint)sender, message);
                }
            }
            catch (Exception e) when (e is SocketException || e is InvalidProtocolBufferException) { }
        }
    }

    public SocketWrapper(IPAddress multicastAddress, int multicastPort)
    {
        _multicastAddress = new IPEndPoint(multicastAddress, multicastPort);
        _multicastSocket = NewMulticastSocket(multicastAddress, multicastPort);
        _unicastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _cancellationToken = _cancellationTokenSource.Token;
    }

    private Socket NewMulticastSocket(IPAddress address, int port)
    {
        Socket mcastSocket = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        mcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        IPAddress localIP = GetLocalIP(address.AddressFamily);
        IPEndPoint endPoint = new(localIP, port);
        mcastSocket.Bind(endPoint);
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            MulticastOption multicastOption = new(address);
            mcastSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
            return mcastSocket;
        }
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            IPv6MulticastOption multicastOption = new(address);
            mcastSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, multicastOption);
            return mcastSocket;
        }
        throw new ArgumentException("Unknown address family for provided arguments.");
    }

    private IPAddress GetLocalIP(AddressFamily addressFamily)
    {
        IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress curAdd in heserver.AddressList)
        {
            if (curAdd.AddressFamily == addressFamily)
            {
                return curAdd;
            }
        }
        throw new Exception($"No address supporting {addressFamily} was found.");
    }

    public void SendUnicast(GameMessage message, IPEndPoint destination)
    {
        try {
            Log.Debug($"Sending {message.TypeCase} message to {message.ReceiverId} on address {destination}.");
            _unicastSocket.SendTo(message.ToByteArray(), destination);
        }
        catch (SocketException) { }
    }

    public void SendMulticast(GameMessage message)
    {
        try
        {
            Log.Debug($"Sending {message.TypeCase} message via multicast.");
            _unicastSocket.SendTo(message.ToByteArray(), _multicastAddress);
        }
        catch (SocketException) { }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _multicastSocket?.Dispose();
        _unicastSocket?.Dispose();
    }
}
