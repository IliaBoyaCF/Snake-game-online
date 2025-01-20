using SnakeOnline.Game.States;
using Snakes;

namespace Network;

public interface IPresenter
{
    public delegate void OnGameStateUpdate(IGameState gameState);

    void OnJoinError(GameMessage.Types.ErrorMsg message);

    void OnError(string message);

    void OnJoinSuccess(int assignedId);

}
