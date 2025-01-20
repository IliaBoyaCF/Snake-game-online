namespace SnakeOnline.Game.States;

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
