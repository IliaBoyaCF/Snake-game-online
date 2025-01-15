namespace Snake_game_online.model.Game.GameState
{
    internal class FieldState : IFieldState
    {
        private readonly List<ISnakeState> _snakeStates;

        private readonly List<IFoodState> _foodStates;

        private readonly (int Width, int Heigth) _size;

        public FieldState(int width, int height, List<ISnakeState> snakeStates, List<IFoodState> foodStates)
        {
            _size = ValueTuple.Create(width, height);
            _snakeStates = snakeStates;
            _foodStates = foodStates;
        }

        public ValueTuple<int, int> GetFieldSize()
        {
            return _size;
        }

        public List<IFoodState> GetFoodState()
        {
            return _foodStates;
        }

        public List<ISnakeState> GetSnakesState()
        {
            return _snakeStates;
        }
    }
}
