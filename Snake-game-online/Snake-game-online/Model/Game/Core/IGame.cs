using Snake_game_online.model.Game.GameState;

namespace SnakeGameOnline.Model.Game.Core;

public interface IGame
{
    public int FieldWidth { get; init; }

    public int FieldHeight { get; init; }

    public int StateDelayMs { get; init; }

    public int FoodStatic {  get; init; }

    public string Name { get; init; }

    public int ActualState {  get; init; }

    public bool CanAddNewPlayer {  get; init; }

    public int NewPlayer(string name);

    int GenerateNewPlayerId();

    void ChangePlayersSnakeDirection(int playerId, ISnakeState.Direction direction);

    void KillPlayer(int playerId);

    IGameState GetState();

    void SetState(IGameState state);

    bool PlayerExists(int playerId);

    bool PlayerExists(string name);

    void Update();
}
