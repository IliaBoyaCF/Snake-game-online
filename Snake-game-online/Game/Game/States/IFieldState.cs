namespace SnakeOnline.Game.States;

public interface IFieldState
{
    List<ISnakeState> GetSnakesState();

    List<IFoodState> GetFoodState();

    (int Width, int Height) GetFieldSize();

}

