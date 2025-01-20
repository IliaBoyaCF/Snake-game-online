namespace SnakeOnline.Game.States;

public interface IGameState
{

    List<IPlayerState> GetIPlayerState();

    IFieldState GetFieldState();

    int StateOrder { get; }

}
