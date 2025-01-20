using SnakeOnline.Game.Core;
using SnakeOnline.Game.States;
using System.Collections.ObjectModel;

namespace SnakeOnline.Game.Entities;

partial class Snake
{

    public delegate void OnDeath(Snake sender, List<Snake> killers);

    public delegate void OnFoodConsume(Snake sender);

    public event OnFoodConsume? FoodConsumed;
    
    public event OnDeath? Death;

    public ISnakeState.Direction Direction 
    { 
        get
        {
            return _currentDirection;
        }
        set
        {
            _nextDirection = value;
        } 
    }

    public ISnakeState.SnakeStatus SnakeStatus { get; set; }

    public bool IsDead { get; set; }

    public int PlayerId { get; }

    public List<BodyPart> BodyParts { get; init; }

    private readonly Head _head;

    private ISnakeState.Direction _currentDirection;

    private ISnakeState.Direction _nextDirection;

    public Snake(int playerId, GameField.Cell head, GameField.Cell body, ISnakeState.Direction direction)
    {
        IsDead = false;
        PlayerId = playerId;

        BodyParts = [];
        BodyParts.Add(new BodyPart(null, body, this));
        _head = new Head(BodyParts[0], head, this);
        _head.Crash += BodyPartCrashHandler;
        BodyParts.Insert(0, _head);

        _currentDirection = direction;
        _nextDirection = direction;
        SnakeStatus = ISnakeState.SnakeStatus.ALIVE;
    }

    public Snake(int playerId, List<GameField.Cell> body, 
        ISnakeState.Direction direction, 
        ISnakeState.SnakeStatus status)
    {
        IsDead = false;
        PlayerId = playerId;

        BodyParts = [new BodyPart(null, body[^1], this)];
        
        for (int i = body.Count - 2; i > 0; --i)
        {
            BodyParts.Add(new BodyPart(BodyParts.Last(), body[i], this));
        }

        _head = new Head(BodyParts.Last(), body[0], this);

        BodyParts.Add(_head);

        BodyParts.Reverse();

        Direction = direction;
        SnakeStatus = status;
    }

    public void Move()
    {
        switch (_currentDirection)
        {
            case ISnakeState.Direction.UP:
                {
                    if (_nextDirection == ISnakeState.Direction.DOWN)
                    {
                        break;
                    }
                    _currentDirection = _nextDirection;
                    break;
                }
            case ISnakeState.Direction.DOWN:
                {
                    if (_nextDirection == ISnakeState.Direction.UP)
                    {
                        break;
                    }
                    _currentDirection = _nextDirection;
                    break;
                }
            case ISnakeState.Direction.LEFT:
                {
                    if (_nextDirection == ISnakeState.Direction.RIGHT)
                    {
                        break;
                    }
                    _currentDirection = _nextDirection;
                    break;
                }
            case ISnakeState.Direction.RIGHT:
                {
                    if (_nextDirection == ISnakeState.Direction.LEFT)
                    {
                        break;
                    }
                    _currentDirection = _nextDirection;
                    break;
                }
        }
        _head.Move(Direction);
    }

    public void CheckCrash()
    {
        _head.CheckCrash();
    }

    public void BodyPartCrashHandler(ReadOnlyCollection<BodyPart> bodyParts)
    {
        if (bodyParts.Count < 2)
        {
            throw new InvalidOperationException("Can't call crash handler on crashing body parts less than one.");
        }
        List<Snake> crashedIntoSnakes = [];
        foreach (BodyPart part in bodyParts)
        {
            Snake snakeICrashedInto = part.Snake;
            if (crashedIntoSnakes.Contains(snakeICrashedInto))
            {
                continue;
            }
            crashedIntoSnakes.Add(snakeICrashedInto);
        }
        IsDead = true;
        Death?.Invoke(this, crashedIntoSnakes);
    }
}
