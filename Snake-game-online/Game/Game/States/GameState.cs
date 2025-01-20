using SnakeOnline.Game.Core;

namespace SnakeOnline.Game.States;

    public class GameState : IGameState
    {

        public int StateOrder { get; }

        private readonly IFieldState _fieldState;
        private readonly List<IPlayerState> _players;

        public GameState(int order, IFieldState fieldState, IReadOnlyList<Player> players)
        {
            StateOrder = order;
            _players = [];
            foreach (Player player in players)
            {
                _players.Add(new PlayerState(player.ID, player.Name, player.Score));
            }
            _fieldState = fieldState;
        }

        public GameState(int order, IFieldState fieldState, List<IPlayerState> players)
        {
            StateOrder = order;
            _players = players;
            _fieldState = fieldState;
        }

        IFieldState IGameState.GetFieldState()
        {
            return _fieldState;
        }

        List<IPlayerState> IGameState.GetIPlayerState()
        {
            return _players;
        }

        public class PlayerState : IPlayerState
        {
            private int _id;
            private string _name;
            private int _score;

            public PlayerState(int id, string name, int score)
            {
                _id = id;
                _name = name;
                _score = score;
            }

            public int GetId()
            {
                return _id;
            }

            public string GetName()
            {
                return _name;
            }

            public int GetScore()
            {
                return _score;
            }
            public IPlayerState.Type GetPlayerType()
            {
                return IPlayerState.Type.HUMAN;
            }
        }

        public override string? ToString()
        {
            string res = string.Empty;
            foreach (IPlayerState playerState in _players)
            {
                res += $"player id: {playerState.GetId()}, name: {playerState.GetName()}, score: {playerState.GetScore()}\n";
            }

            foreach (ISnakeState snake in _fieldState.GetSnakesState())
            {
                res += $"snake player_id: {snake.GetPlayerId()}, direction: {snake.GetDirection()}, coords: ({snake.GetBody().First().GetCoordinates().GetX()}, {snake.GetBody().First().GetCoordinates().GetY()})\n";
            }

            foreach (IFoodState food in _fieldState.GetFoodState())
            {
                res += $"food in ({food.GetCoordinates().GetX()}, {food.GetCoordinates().GetY()})\n";
            }

            return res;
        }
    }
