namespace Snake_game_online.model.Game.GameState;

public interface ILocatable
{
    interface ICoordinates
    {
        int GetX();

        int GetY();
    }
    ICoordinates GetCoordinates();
}
