﻿namespace SnakeOnline.Game.States;

public interface ISnakeState
{

    interface IBody : ILocatable;

    public enum SnakeStatus
    {
        ALIVE,
        ZOMBIE,
    }

    public enum Direction
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
    }

    SnakeStatus GetStatus();

    int GetPlayerId();

    List<IBody> GetBody();

    Direction GetDirection();
}
