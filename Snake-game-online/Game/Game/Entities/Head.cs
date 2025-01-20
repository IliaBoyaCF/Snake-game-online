using SnakeOnline.Game.Core;
using SnakeOnline.Game.States;
using System.Collections.ObjectModel;

namespace SnakeOnline.Game.Entities;

partial class Snake
{
    public class Head : BodyPart
    {
        public Head(BodyPart? next, GameField.Cell position, Snake snake) : base(next, position, snake) 
        {
            FoodConsumed = snake.FoodConsumed;
        }

        public delegate void OnCrash(ReadOnlyCollection<BodyPart> crashedInto);

        public event OnCrash? Crash;

        public event OnFoodConsume? FoodConsumed;

        public override void Move(GameField.Cell newPosition)
        {
            if (!newPosition.HasFood)
            {
                base.Move(newPosition);
            }
            else
            {
                BodyPart next = new BodyPart(_next, _position, _snake);
                _snake.BodyParts.Add(next);
                _next = next;
                _position.Remove(this);
                _position = newPosition;
                _position.Place(this);
                _snake.FoodConsumed?.Invoke(_snake);
            }
        }

        public void CheckCrash()
        {
            ReadOnlyCollection<BodyPart> bodyParts = _position.BodyParts;
            if (bodyParts.Count > 1)
            {
                Crash?.Invoke(bodyParts);
            }
        }

        public void Move(ISnakeState.Direction direction)
        {
            GameField.Cell cellToMove = direction switch
            {
                ISnakeState.Direction.UP => _position.GetUpperNeighbor(),
                ISnakeState.Direction.RIGHT => _position.GetRightNeighbor(),
                ISnakeState.Direction.DOWN => _position.GetBottomNeighbor(),
                ISnakeState.Direction.LEFT => _position.GetLeftNeighbor(),
                _ => throw new ArgumentOutOfRangeException(nameof(direction)),
            };
            Move(cellToMove);
        }
    }
}
