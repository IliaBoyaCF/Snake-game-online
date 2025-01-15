using static Snake_game_online.model.Game.GameState.ILocatable;

namespace Snake_game_online.model.Game.GameState
{
    internal class Coordinates : ICoordinates
    {
        private readonly Tuple<int, int> _cords;
        public Coordinates(Tuple<int, int> cords)
        {
            _cords = cords;
        }

        public int GetX()
        {
            return _cords.Item1;
        }

        public int GetY()
        {
            return _cords.Item2;
        }
    }
}
