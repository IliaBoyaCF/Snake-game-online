using Snake_game_online.model.Game.GameState;

namespace SnakeGameOnline.Model
{
    partial class Snake
    {
        public class SnakeState : ISnakeState
        {
            private int _id;
            private List<ISnakeState.IBody> _body;
            private ISnakeState.Direction _direction;
            private ISnakeState.SnakeStatus _snakeStatus;

            public SnakeState(int playerId, List<BodyPart> bodyParts, 
                ISnakeState.Direction direction, 
                ISnakeState.SnakeStatus status) 
            { 
                _id = playerId;
                _direction = direction;
                _snakeStatus = status;
                _body = [];
                foreach (BodyPart bodyPart in bodyParts)
                {
                    if (bodyPart is Head)
                    {
                        _body.Insert(0, new BodyPart.Body(bodyPart));
                        continue;
                    }
                    _body.Add(new BodyPart.Body(bodyPart));
                }
            }

            public SnakeState(int playerId, List<ILocatable.ICoordinates> bodyParts,
                ISnakeState.Direction direction,
                ISnakeState.SnakeStatus status)
            {
                _id = playerId;
                _direction = direction;
                _snakeStatus = status;
                _body = [];
                foreach (ILocatable.ICoordinates bodyPart in bodyParts)
                {
                    if (bodyPart is Head)
                    {
                        _body.Insert(0, new BodyPart.Body(bodyPart));
                        continue;
                    }
                    _body.Add(new BodyPart.Body(bodyPart));
                }
            }

            public int GetPlayerId()
            {
                return _id;
            }

            List<ISnakeState.IBody> ISnakeState.GetBody()
            {
                return _body;
            }

            ISnakeState.Direction ISnakeState.GetDirection()
            {
                return _direction;
            }

            ISnakeState.SnakeStatus ISnakeState.GetStatus()
            {
                return _snakeStatus;
            }
        }
    }
}
