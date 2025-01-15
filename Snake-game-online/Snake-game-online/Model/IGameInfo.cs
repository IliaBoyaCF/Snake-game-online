using Snake_game_online.model.Game.GameState;

namespace SnakeGameOnline.Model;

public interface IGameInfo
{

    public interface IGameConfig
    {
        int FieldWidth { get; }
        int FieldHeight { get; }
        int FoodStatic { get; }
        int StateDelay_ms { get; }
    }

    string Name { get; }
    
    List<IPlayerState> Players { get; }

    IGameConfig GameConfig { get; }

    bool CanJoin { get; }
}
