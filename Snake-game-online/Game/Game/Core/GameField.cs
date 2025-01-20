using Game.Game.States;
using SnakeOnline.Game.Entities;
using SnakeOnline.Game.States;
using System.Collections.ObjectModel;

namespace SnakeOnline.Game.Core;

public partial class GameField
{

    public class FoodState : IFoodState
    {
        private readonly Coordinates _cords;

        public FoodState(Coordinates cords)
        {
            _cords = cords;
        }

        ILocatable.ICoordinates ILocatable.GetCoordinates()
        {
            return _cords;
        }
    }

    public int Width { get; init; }

    public int Height { get; init; }

    public ReadOnlyDictionary<int, Snake> Snakes
    {
        get
        {
            return _snakes.AsReadOnly();
        }
    }

    public int FoodCount
    {
        get
        {
            return _cellsWithFood.Count;
        }
    }

    private readonly Dictionary<Tuple<int, int>, Cell> _cells = [];

    private readonly List<Cell> _cellsWithFood = [];

    private readonly Dictionary<int, Snake> _snakes = [];

    private readonly Random _random = new Random();

    public GameField(int fieldWidth, int fieldHeight)
    {
        Width = fieldWidth;
        Height = fieldHeight;
        GenerateCells();
    }

    private void GenerateCells()
    {
        for (int i = 0; i < Width; ++i)
        {
            for (int j = 0; j < Height; ++j)
            {
                _cells.Add(Tuple.Create(i, j), new Cell(i, j, this));
            }
        }
    }

    private Cell GetBottomNeighborFor(Cell cell)
    {
        return _cells[new Tuple<int, int>(cell.X, (cell.Y + 1) % Height)];
    }

    private Cell GetUpperNeighborFor(Cell cell)
    {
        return _cells[new Tuple<int, int>(cell.X, (cell.Y + Height - 1) % Height)];
    }

    private Cell GetRightNeighborFor(Cell cell)
    {
        return _cells[new Tuple<int, int>((cell.X + 1) % Width, cell.Y)];
    }

    private Cell GetLeftNeighborFor(Cell cell)
    {
        return _cells[new Tuple<int, int>((cell.X + Width - 1) % Width, cell.Y)];
    }

    public void SpawnFood(int count)
    {
        if (_cellsWithFood.Count == _cells.Count)
        {
            throw new ArgumentOutOfRangeException("Can't add more food");
        }
        List<Cell> freeCells = new List<Cell>(_cells.Values);
        freeCells.RemoveAll((cell) =>
        {
            return cell.HasFood || cell.BodyParts.Count > 0;
        });
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, freeCells.Count);
        if (freeCells.Count > count)
        {
            Cell[] freeCellsArray = freeCells.ToArray();
            _random.Shuffle(freeCellsArray);
            Array.Resize(ref freeCellsArray, count);
            freeCells = freeCellsArray.ToList();
        }
        for (int i = 0; i < count; i++)
        {
            freeCells[i].HasFood = true;
        }
        _cellsWithFood.AddRange(freeCells);
    }

    public void Update()
    {
        foreach (Snake snake in _snakes.Values)
        {
            snake.Move();
        }
        _cellsWithFood.RemoveAll((cell) =>
        {
            bool foodEaten = cell.BodyParts.Count > 0;
            if (foodEaten)
            {
                cell.HasFood = false;
            }
            return foodEaten;
        });
        foreach (Snake snake in _snakes.Values)
        {
            snake.CheckCrash();
        }
        List<int> deadSnakes = [];
        foreach (Snake snake in _snakes.Values)
        {
            if (!snake.IsDead)
            {
                continue;
            }
            foreach (Snake.BodyPart bodyPart in snake.BodyParts)
            {
                Cell cell = _cells[Tuple.Create(bodyPart.Position.X, bodyPart.Position.Y)];
                cell.Remove(bodyPart);
                if (_random.Next() % 2 == 0)
                {
                    cell.HasFood = true;
                    _cellsWithFood.Add(cell);
                }
            }
            deadSnakes.Add(snake.PlayerId);
        }
        foreach (int i in deadSnakes)
        {
            _snakes.Remove(i);
        }
    }

    internal Snake PlaceNewSnake(int newPlayerId)
    {
        List<Cell> availableToSpawn = FindCellsForSpawn();
        if (availableToSpawn.Count == 0)
        {
            throw new InvalidOperationException("No space for a new snake.");
        }
        Cell cellWithHead = availableToSpawn[_random.Next(availableToSpawn.Count)];
        Snake snake;
        switch (_random.Next(4))
        {
            case 0:
                {
                    Cell cellWithBody = cellWithHead.GetLeftNeighbor();
                    snake = new Snake(newPlayerId, cellWithHead, cellWithBody, ISnakeState.Direction.RIGHT);
                    break;
                }
            case 1:
                {
                    Cell cellWithBody = cellWithHead.GetRightNeighbor();
                    snake = new Snake(newPlayerId, cellWithHead, cellWithBody, ISnakeState.Direction.LEFT);
                    break;
                }
            case 2:
                {
                    Cell cellWithBody = cellWithHead.GetUpperNeighbor();
                    snake = new Snake(newPlayerId, cellWithHead, cellWithBody, ISnakeState.Direction.DOWN);
                    break;
                }
            case 3:
                {
                    Cell cellWithBody = cellWithHead.GetBottomNeighbor();
                    snake = new Snake(newPlayerId, cellWithHead, cellWithBody, ISnakeState.Direction.UP);
                    break;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException("Unknown direction.");
                }
        }
        _snakes.Add(newPlayerId, snake);
        return snake;
    }

    private List<Cell> FindCellsForSpawn()
    {
        List<Cell> occupiedCells = GetOccupiedCells();

        List<Cell> cells = new List<Cell>(_cells.Values);
        foreach (Cell occupiedCell in occupiedCells)
        {
            List<Cell> neighbors = GetNeighbors(occupiedCell);
            cells.RemoveAll(neighbors.Contains);
        }
        return cells;
    }

    private List<Cell> GetOccupiedCells()
    {
        List<Cell> occupiedCells = [];
        foreach (var item in _cells.Values)
        {
            if (item.HasFood || item.BodyParts.Count != 0)
            {
                occupiedCells.Add(item);
            }
        }

        return occupiedCells;
    }


    // Returns list with cells located in square 5x5 with given cell in the center.
    private List<Cell> GetNeighbors(Cell cell)
    {
        List<Cell> cells = [];
        AddLine(cell, cells);
        AddLine(cell.GetUpperNeighbor(), cells);
        AddLine(cell.GetBottomNeighbor(), cells);
        return cells;
    }
    private void AddLine(Cell cell, List<Cell> cells)
    {
        cells.Add(cell);
        cells.Add(cell.GetLeftNeighbor());
        cells.Add(cell.GetLeftNeighbor().GetLeftNeighbor());
        cells.Add(cell.GetRightNeighbor());
        cells.Add(cell.GetRightNeighbor().GetRightNeighbor());
    }

    internal IFieldState GetState()
    {
        List<ISnakeState> snakeStates = [];
        foreach (Snake snake in _snakes.Values)
        {
            snakeStates.Add(new Snake.SnakeState(snake.PlayerId, snake.BodyParts, snake.Direction, snake.SnakeStatus));
        }
        List<IFoodState> foodStates = [];
        foreach (Cell cell in _cellsWithFood)
        {
            foodStates.Add(new FoodState(new Coordinates(Tuple.Create(cell.X, cell.Y))));
        }
        return new FieldState(Width, Height, snakeStates, foodStates);
    }

    internal void SetState(IFieldState fieldState)
    {
        if (fieldState.GetFieldSize().Item1 != Width || fieldState.GetFieldSize().Item2 != Height)
        {
            throw new InvalidOperationException("Provided state does not match field configuration.");
        }
        _cells.Clear();
        _cellsWithFood.Clear();
        _snakes.Clear();
        GenerateCells();
        SetFood(fieldState.GetFoodState());
        SetSnakes(fieldState.GetSnakesState());
    }

    private void SetSnakes(List<ISnakeState> snakeStates)
    {
        foreach (ISnakeState snakeState in snakeStates)
        {
            Snake snake = SnakeFromState(snakeState);
            _snakes.Add(snake.PlayerId, snake);
        }
    }

    private Snake SnakeFromState(ISnakeState snakeState)
    {
        List<Cell> body = [];
        foreach (ISnakeState.IBody bodyPart in snakeState.GetBody())
        {
            body.Add(_cells[Tuple.Create(bodyPart.GetCoordinates().GetX(), bodyPart.GetCoordinates().GetY())]);
        }
        return new Snake(snakeState.GetPlayerId(), body, snakeState.GetDirection(), snakeState.GetStatus());
    }

    private void SetFood(List<IFoodState> foodState)
    {
        foreach (IFoodState food in foodState)
        {
            Cell cellWithFood = _cells[Tuple.Create(food.GetCoordinates().GetX(), food.GetCoordinates().GetY())];
            cellWithFood.HasFood = true;
            _cellsWithFood.Add(cellWithFood);
        }
    }

    internal bool CanPlaceNewSnake()
    {
        return FindCellsForSpawn().Count > 0;
    }
}