namespace Snake_game_online.model.Game.GameState;

public interface IPlayerState
{
    public enum Type
    {
        HUMAN,
        ROBOT,
    }

    string GetName();

    int GetId();

    int GetScore();

    Type GetPlayerType();
}
