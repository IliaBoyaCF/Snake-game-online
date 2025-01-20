using Game.Game.States;
using Google.Protobuf.Collections;
using SnakeOnline.Game.Core;
using SnakeOnline.Game.States;
using Snakes;
using System.Diagnostics;
using System.Net;
using static SnakeOnline.Game.Entities.Snake;
using static SnakeOnline.Game.States.GameState;

namespace Network.Node;

public abstract class ConnectedNode : Node
{

    protected ConnectedNode(NodeContext context) : base(context)
    {
        _context.NodeRadar.Start();
    }

    public override void ChangePlayerDirection(int id, ISnakeState.Direction direction)
    {
        GamePlayer master = _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode());
        _context.MessageDeliveryController.Deliver(new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            ReceiverId = master.Id,
            SenderId = _context.MyId,
            Steer = new GameMessage.Types.SteerMsg()
            {
                Direction = ToProtobufDirection(direction),
            }
        }, GetIpEndPoint(master));
    }

    // If message got from master node and current known state is lower than received, then sets game state to received.
    // !!! Does not send AckMessage in response to state!!!
    public override void OnStateReceived(IPEndPoint sender, GameMessage message)
    {
        if (_context.MessageDeliveryController.GetPending(sender).Find((m) => m.MsgSeq == message.MsgSeq && m.TypeCase == GameMessage.TypeOneofCase.Ack) != null)
        {
            return;
        }
        Debug.Assert(message.TypeCase == GameMessage.TypeOneofCase.State);
        if (!IsMessageFromMaster(sender, message))
        {
            //SendWarningToCheater(sender);
            return;
        }
        if (_context.Game.ActualState > message.State.State.StateOrder)
        {
            return;
        }
        IGameState gameState = ParseGameState(message.State);
        _context.SynchronizedOnGame(
        _ =>
        {
            _context.Game.SetState(gameState);
        });
        _context.OnGameStateUpdate(gameState);
        UpdatePlayersInfo(message);
        SendAckTo(sender, message, message.SenderId);
    }

    private void UpdatePlayersInfo(GameMessage message)
    {
        _context.SynchronizedOnNodes(_ =>
        {
            foreach (GamePlayer gamePlayer in message.State.State.Players.Players)
            {
                if (!_context.Nodes.Contains(gamePlayer.Id))
                {
                    _context.Nodes.Add(gamePlayer);
                    continue;
                }
                if (gamePlayer.Role == NodeRole.Master)
                {
                    GamePlayer master = _context.Nodes.GetMasterNode();
                    _context.Nodes.Remove(gamePlayer.Id);
                    _context.Nodes.Add(new GamePlayer(gamePlayer)
                    {
                        IpAddress = master.IpAddress,
                        Port = master.Port,
                    });
                    continue;
                }
                _context.Nodes.Remove(gamePlayer.Id);
                _context.Nodes.Add(gamePlayer);
            }
        });
    }

    private IGameState ParseGameState(GameMessage.Types.StateMsg state)
    {
        FieldState fieldState = ParseFieldState(state);
        List<IPlayerState> playerStates = ParsePlayerStates(state.State.Players);
        return new SnakeOnline.Game.States.GameState(state.State.StateOrder, fieldState, playerStates);
    }

    public static List<IPlayerState> ParsePlayerStates(GamePlayers players)
    {
        List<IPlayerState> playerStates = [];
        foreach (GamePlayer player in players.Players)
        {
            playerStates.Add(new PlayerState(player.Id, player.Name, player.Score));
        }
        return playerStates;
    }

    private FieldState ParseFieldState(GameMessage.Types.StateMsg state)
    {
        return new FieldState(_context.Game.FieldWidth, _context.Game.FieldHeight, ParseSnakeStates(state.State.Snakes), ParseFoodStates(state.State.Foods));
    }

    private List<IFoodState> ParseFoodStates(RepeatedField<Snakes.GameState.Types.Coord> foods)
    {
        List<IFoodState> foodStates = [];
        foreach (Snakes.GameState.Types.Coord food in foods)
        {
            foodStates.Add(new GameField.FoodState(new Coordinates(Tuple.Create(food.X, food.Y))));
        }
        return foodStates;
    }

    private List<ISnakeState> ParseSnakeStates(RepeatedField<Snakes.GameState.Types.Snake> snakes)
    {
        List<ISnakeState> snakeStates = [];
        foreach (Snakes.GameState.Types.Snake snake in snakes)
        {
            snakeStates.Add(ParseSnakeState(snake));
        }
        return snakeStates;
    }

    private ISnakeState ParseSnakeState(Snakes.GameState.Types.Snake snake)
    {
        List<ILocatable.ICoordinates> body = [];
        foreach (Snakes.GameState.Types.Coord bodyCoords in snake.Points)
        {
            body.Add(new Coordinates(Tuple.Create(bodyCoords.X, bodyCoords.Y)));
        }
        ISnakeState.SnakeStatus snakeStatus = GetSnakeStatus(snake.State);
        ISnakeState.Direction direction = GetSnakeDirection(snake.HeadDirection);
        return new SnakeState(snake.PlayerId, body, direction, snakeStatus);
    }

    private ISnakeState.Direction GetSnakeDirection(Direction headDirection)
    {
        return headDirection switch
        {
            Direction.Up => ISnakeState.Direction.UP,
            Direction.Down => ISnakeState.Direction.DOWN,
            Direction.Left => ISnakeState.Direction.LEFT,
            Direction.Right => ISnakeState.Direction.RIGHT,
            _ => throw new ArgumentException("Unknown Direction")
        };
    }

    private ISnakeState.SnakeStatus GetSnakeStatus(Snakes.GameState.Types.Snake.Types.SnakeState state)
    {
        return state switch
        {
            Snakes.GameState.Types.Snake.Types.SnakeState.Alive => ISnakeState.SnakeStatus.ALIVE,
            Snakes.GameState.Types.Snake.Types.SnakeState.Zombie => ISnakeState.SnakeStatus.ZOMBIE,
            _ => throw new ArgumentException("Unknown status."),
        };
    }

    // Checks if the message come from current known master node.
    protected bool IsMessageFromMaster(IPEndPoint sender, GameMessage message)
    {
        GamePlayer? masterNode = _context.SynchronizedOnNodes(_ => _context.Nodes.GetMasterNode());
        if (masterNode == null)
        {
            return false;
        }
        return masterNode.IpAddress == sender.Address.ToString() && masterNode.Port == sender.Port && masterNode.Id == message.SenderId;
    }

    public override void OnSteerReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnJoinReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnDiscoverReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }
}
