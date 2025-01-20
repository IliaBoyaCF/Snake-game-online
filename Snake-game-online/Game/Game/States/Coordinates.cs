using static SnakeOnline.Game.States.ILocatable;

namespace SnakeOnline.Game.States;

public class Coordinates : ICoordinates
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
