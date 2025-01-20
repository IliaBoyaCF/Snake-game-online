namespace SnakeOnline.Game.States;

public interface ILocatable
{
    interface ICoordinates
    {
        int GetX();

        int GetY();
    }
    ICoordinates GetCoordinates();
}
