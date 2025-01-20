using SnakeOnline.Game.Entities;
using SnakeOnline.Game.States;

namespace SnakeOnline.Game.Core;

public class Player
{
    private readonly int _id;
    private readonly string _name;

    private readonly Snake _snake;

    private int _score = 0;

    public Player(int id, string name, Snake snake)
    {
        _id = id;
        _name = name;
        _snake = snake;
    }

    public Player(int id, string name, int score, Snake snake)
    {
        _id = id;
        _name = name;
        _snake = snake;
        _score = score;
    }

    public string Name
    {
        get
        {
            return _name;
        }
    }
    public int Score
    {
        get
        {
            return _score;
        }
    }
    public int ID
    {
        get
        {
            return _id;
        }
    }

    public void ChangeSnakeDirection(ISnakeState.Direction direction)
    {
        if (IsNewDirectionIllegal(_snake.Direction, direction))
        {
            return;
        }
        _snake.Direction = direction;
    }

    private static bool IsNewDirectionIllegal(ISnakeState.Direction currentDirection, ISnakeState.Direction newDirection)
    {
        if (currentDirection == ISnakeState.Direction.UP && newDirection == ISnakeState.Direction.DOWN)
        {
            return true;
        }
        if (currentDirection == ISnakeState.Direction.DOWN && newDirection == ISnakeState.Direction.UP)
        {
            return true;
        }
        if (currentDirection == ISnakeState.Direction.LEFT && newDirection == ISnakeState.Direction.RIGHT)
        {
            return true;
        }
        if (currentDirection == ISnakeState.Direction.RIGHT && newDirection == ISnakeState.Direction.LEFT)
        {
            return true;
        }
        return false;
    }

    public void UnbindSnake()
    {
        _snake.SnakeStatus = ISnakeState.SnakeStatus.ZOMBIE;
    }

    internal void AddScore(int s_scoreForKill)
    {
        _score += s_scoreForKill;
    }
}
