namespace Snake_game_online.model.Game.GameState;

public interface IFieldState
{
    List<ISnakeState> GetSnakesState();

    List<IFoodState> GetFoodState();

    (int Width, int Height) GetFieldSize();

}

