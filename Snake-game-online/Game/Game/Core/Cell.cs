using System.Collections.ObjectModel;
using static SnakeOnline.Game.Entities.Snake;

namespace SnakeOnline.Game.Core;

public partial class GameField
{
    public class Cell
    {
        public int X { get; }

        public int Y { get; }

        private readonly GameField _field;

        public ReadOnlyCollection<BodyPart> BodyParts
        {
            get
            {
                return _bodyParts.AsReadOnly();
            }
        }

        public bool HasFood { get; set; }

        private List<BodyPart> _bodyParts;

        public Cell(int x, int y, GameField field)
        {
            _bodyParts = [];
            _field = field;
            X = x;
            Y = y;
        }

        public void Place(BodyPart bodyPart)
        {
            if (_bodyParts.Contains(bodyPart))
            {
                return;
            }
            _bodyParts.Add(bodyPart);
        }

        public void Remove(BodyPart bodyPart)
        {
            if (!_bodyParts.Remove(bodyPart))
            {
                throw new ArgumentException("This body part didn't presented in the cell.");
            }
        }

        public Cell GetLeftNeighbor()
        {
            return _field.GetLeftNeighborFor(this);
        }

        public Cell GetRightNeighbor()
        {
            return _field.GetRightNeighborFor(this);
        }

        public Cell GetUpperNeighbor()
        {
            return _field.GetUpperNeighborFor(this);
        }

        public Cell GetBottomNeighbor()
        {
            return _field.GetBottomNeighborFor(this);
        }
    }
}