using Snake_game_online.model.Game.GameState;
using SnakeGameOnline.Model.Game.Core;

namespace SnakeGameOnline.Model
{
    partial class Snake
    {
        public class BodyPart
        {

            public class Body : ISnakeState.IBody
            {
                ILocatable.ICoordinates _cords;

                public Body(BodyPart bodyPart)
                {
                    _cords = new Coordinates(Tuple.Create(bodyPart._position.X, bodyPart._position.Y));
                }
                public Body(ILocatable.ICoordinates coordinates)
                {
                    _cords = coordinates;
                }

                ILocatable.ICoordinates ILocatable.GetCoordinates()
                {
                    return _cords;
                }
            }

            protected BodyPart? _next;

            protected GameField.Cell _position;

            protected Snake _snake;

            public Snake Snake
            {
                get
                {
                    return _snake;
                }
            }

            public GameField.Cell Position
            {
                get
                {
                    return _position;
                }
            }

            public BodyPart(BodyPart? next, GameField.Cell position, Snake snake)
            {
                _next = next;
                _position = position;
                _snake = snake;
                _position.Place(this);
            }

            public virtual void Move(GameField.Cell newPosition)
            {
                GameField.Cell oldPosition = _position;
                
                oldPosition.Remove(this);
                _position = newPosition;

                newPosition.Place(this);
                _next?.Move(oldPosition);
            }
        }
    }
}
