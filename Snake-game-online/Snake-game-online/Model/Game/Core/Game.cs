using Snake_game_online.model.Game.GameState;

namespace SnakeGameOnline.Model.Game.Core;

partial class Game : IGame
{

    private class PlayerStorage
    {
        public int Count { 
            get
            {
                return _playersById.Count;
            } 
        }

        public IReadOnlyList<Player> Values
        {
            get
            {
                return _playersById.Values.ToList().AsReadOnly();
            }
        }

        private readonly Dictionary<int, Player> _playersById = [];
        private readonly Dictionary<string, Player> _playersByName = [];

        public Player GetById(int id)
        {
            return _playersById[id];
        }

        public void Add(Player player)
        {
            if (_playersById.ContainsKey(player.ID))
            {
                throw new InvalidOperationException("Already contains this element.");
            }
            _playersById.Add(player.ID, player);
            _playersByName.Add(player.Name, player);
        }

        public void Remove(int id)
        {
            Player player = _playersById[id];
            _playersById.Remove(player.ID);
            _playersByName.Remove(player.Name);
        }

        public void Remove(string name)
        {
            Player player = _playersByName[name];
            _playersById.Remove(player.ID);
            _playersByName.Remove(player.Name);
        }

        public void Clear()
        {
            _playersByName.Clear();
            _playersById.Clear();
        }

        public bool Contains(int id)
        {
            return _playersById.ContainsKey(id);
        }

        public bool Contains(string name)
        {
            return _playersByName.ContainsKey(name);
        }

        public Player GetByName(string name)
        {
            return _playersByName[name];
        }

    }

    public const int s_scoreForKill = 1;
    public const int s_scoreForEatenFood = 1;

    public int FoodStatic { get; init; }

    public string Name { get { return _name; } init { } }

    public int FieldWidth { get; init; }
    public int FieldHeight { get; init; }
    public int ActualState { get => _stateOrder; init {}}
    public bool CanAddNewPlayer { 
        get
        {
            return _field.CanPlaceNewSnake();
        }
        init {}
    }

    public int StateDelayMs { get; init; }

    private readonly GameField _field;

    private readonly PlayerStorage _players = new PlayerStorage();

    private readonly string _name;

    private int _nextIdToGive = 0;

    private int _stateOrder = 0;

    public Game(string name, int stateDelayMs, int fieldWidth, int fieldHeight, int foodStatic)
    {
        StateDelayMs = stateDelayMs;
        _name = name;
        FoodStatic = foodStatic;
        FieldWidth = fieldWidth;
        FieldHeight = fieldHeight;
        _field = new GameField(fieldWidth,  fieldHeight);
    }

    public void ChangePlayersSnakeDirection(int playerId, ISnakeState.Direction direction)
    {
        if (!_players.Contains(playerId))
        {
            throw new KeyNotFoundException();
        }
        _players.GetById(playerId).ChangeSnakeDirection(direction);
    }

    public IGameState GetState()
    {
        IFieldState fieldState = _field.GetState();
        return new Snake_game_online.model.Game.GameState.Game.GameState(_stateOrder++, fieldState, _players.Values);
    }

    public void KillPlayer(int playerId)
    {
        _players.GetById(playerId).UnbindSnake();
        _players.Remove(playerId);
    }

    public int NewPlayer(string name)
    {
        int newPlayerId = GenerateNewPlayerId();
        Snake playersSnake = _field.PlaceNewSnake(newPlayerId);

        playersSnake.Death += OnSnakeDeath;
        playersSnake.FoodConsumed += OnSnakeAteFood;

        Player newPlayer = new Player(newPlayerId, name, playersSnake);
        _players.Add(newPlayer);
        return newPlayerId;
    }

    public void SetState(IGameState state)
    {
        //if (_stateOrder >= state.StateOrder)
        //{
        //    return;
        //}
        _stateOrder = state.StateOrder;
        _field.SetState(state.GetFieldState());
        _players.Clear();
        foreach (IPlayerState playerState in state.GetIPlayerState())
        {
            _players.Add(new Player(playerState.GetId(), playerState.GetName(),
                playerState.GetScore(), _field.Snakes[playerState.GetId()]));
            _nextIdToGive = _nextIdToGive < playerState.GetId() + 1 ? playerState.GetId() + 1 : _nextIdToGive;
        }
    }

    public void Update()
    {
        _field.Update();
        if (_field.FoodCount < _players.Count + FoodStatic)
        {
            int needToSpawnFoodCount = _players.Count + FoodStatic - _field.FoodCount;
            try
            {
                _field.SpawnFood(needToSpawnFoodCount);
            }
            catch (ArgumentOutOfRangeException) { }
        }
    }

    private void OnSnakeDeath(Snake dead, List<Snake> killers)
    {
        _players.Remove(dead.PlayerId);
        foreach (Snake snake in killers)
        {
            if (snake.PlayerId == dead.PlayerId)
            {
                continue;
            }
            if (!_players.Contains(snake.PlayerId))
            {
                continue;
            }
            _players.GetById(snake.PlayerId).AddScore(s_scoreForKill);
        }
    }

    private void OnSnakeAteFood(Snake snake)
    {
        if (!_players.Contains(snake.PlayerId))
        {
            return;
        }
        _players.GetById(snake.PlayerId).AddScore(s_scoreForEatenFood);
    }

    public bool PlayerExists(int playerId)
    {
        return _players.Contains(playerId);
    }

    public bool PlayerExists(string name)
    {
        return _players.Contains(name);
    }

    public int GenerateNewPlayerId()
    {
        return _nextIdToGive++;
    }
}
