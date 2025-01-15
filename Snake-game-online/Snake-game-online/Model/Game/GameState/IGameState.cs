namespace Snake_game_online.model.Game.GameState;

public interface IGameState
{

    List<IPlayerState> GetIPlayerState();

    IFieldState GetFieldState();

    int StateOrder { get; }

}
