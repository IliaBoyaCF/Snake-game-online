using SnakeOnline.Game.States;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SnakeOnline;

public partial class GameWindow
{
    private class CanvasDrawer
    {
        private readonly Random _random = new Random();
        private readonly Dictionary<int, SolidColorBrush> _playersColors = [];
        private readonly Canvas _canvas;

        private readonly ValueTuple<int, int> _gameFieldSize;

        private double _cellWidth;
        private double _cellHeight;


        public CanvasDrawer(Canvas canvas, Tuple<int, int> gameFieldSize)
        {
            _canvas = canvas;
            _gameFieldSize = ValueTuple.Create(gameFieldSize.Item1, gameFieldSize.Item2);
        }

        public void DrawField(IFieldState fieldState)
        {
            RecalculateCellSize();
            DrawGrid(fieldState.GetFieldSize());
            DrawFood(fieldState.GetFoodState());
            DrawSnakes(fieldState.GetSnakesState());
        }

        private void DrawSnakes(List<ISnakeState> snakeStates)
        {
            foreach (var snakeState in snakeStates)
            {
                DrawSnake(snakeState);
            }
        }

        private void DrawSnake(ISnakeState snakeState)
        {
            SolidColorBrush snakeColor = SelectSnakeColor(snakeState);
            List<ISnakeState.IBody> snakeBody = snakeState.GetBody();
            ISnakeState.IBody snakeHead = snakeBody.First();
            DrawSnakeHead(snakeHead, snakeState.GetStatus(), snakeState.GetDirection(), snakeColor);
            snakeBody.RemoveAt(0);
            foreach (var body in snakeBody)
            {
                Rectangle bodyRect = new Rectangle()
                {
                    Width = _cellWidth,
                    Height = _cellHeight,
                    Fill = snakeColor,
                };
                _canvas.Children.Add(bodyRect);
                Canvas.SetLeft(bodyRect, body.GetCoordinates().GetX() * _cellWidth);
                Canvas.SetTop(bodyRect, body.GetCoordinates().GetY() * _cellHeight);
            }
        }

        private void DrawSnakeHead(ISnakeState.IBody snakeHead, ISnakeState.SnakeStatus snakeStatus, ISnakeState.Direction direction, SolidColorBrush color)
        {
            Rectangle bodyRect = new Rectangle()
            {
                Width = _cellWidth,
                Height = _cellHeight,
                Fill = color,
            };
            _canvas.Children.Add(bodyRect);
            Canvas.SetLeft(bodyRect, snakeHead.GetCoordinates().GetX() * _cellWidth);
            Canvas.SetTop(bodyRect, snakeHead.GetCoordinates().GetY() * _cellHeight);

            SolidColorBrush headColor = snakeStatus == ISnakeState.SnakeStatus.ALIVE ? Brushes.Gold : Brushes.Green;

            Ellipse ellipse = new Ellipse()
            {
                Fill = headColor,
                Width = _cellWidth,
                Height = _cellHeight,
            };
            _canvas.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, snakeHead.GetCoordinates().GetX() * _cellWidth);
            Canvas.SetTop(ellipse, snakeHead.GetCoordinates().GetY() * _cellHeight);

        }

        private SolidColorBrush SelectSnakeColor(ISnakeState snakeState)
        {
            if (_playersColors.ContainsKey(snakeState.GetPlayerId()))
            {
                return _playersColors[snakeState.GetPlayerId()];
            }
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)_random.Next(1, 255),
                              (byte)_random.Next(1, 255), (byte)_random.Next(1, 233)));
            _playersColors.Add(snakeState.GetPlayerId(), brush);
            return brush;
        }

        private void RecalculateCellSize()
        {
            _cellWidth = _canvas.ActualWidth / _gameFieldSize.Item1;
            _cellHeight = _canvas.ActualHeight / _gameFieldSize.Item2;
        }

        private void DrawFood(List<IFoodState> foodStates)
        {
            foreach (IFoodState foodState in foodStates)
            {
                Ellipse ellipse = new Ellipse()
                {
                    Fill = Brushes.Red,
                    Width = _cellWidth,
                    Height = _cellHeight,
                };
                _canvas.Children.Add(ellipse);
                Canvas.SetLeft(ellipse, foodState.GetCoordinates().GetX() * _cellWidth);
                Canvas.SetTop(ellipse, foodState.GetCoordinates().GetY() * _cellHeight);
            }
        }

        private void DrawGrid(ValueTuple<int, int> fieldSize)
        {
            FillCanvas(Brushes.Black);

            const double gridThickness = 3;

            for (int i = 1; i < fieldSize.Item1; i++)
            {
                Line line = new Line
                {
                    Stroke = Brushes.White,
                    StrokeThickness = gridThickness,
                    X1 = _cellWidth * i,
                    Y1 = 0,
                    X2 = _cellWidth * i,
                    Y2 = _canvas.ActualHeight
                };
                _canvas.Children.Add(line);
            }
            for (int i = 1; i < fieldSize.Item2; i++)
            {
                Line line = new Line
                {
                    Stroke = Brushes.White,
                    StrokeThickness = gridThickness,
                    X1 = 0,
                    Y1 = _cellHeight * i,
                    X2 = _canvas.ActualWidth,
                    Y2 = _cellHeight * i
                };
                _canvas.Children.Add(line);
            }
        }

        private void FillCanvas(SolidColorBrush colorBrush)
        {
            Rectangle rectangle = new Rectangle
            {
                Height = _canvas.ActualHeight,
                Width = _canvas.ActualWidth,
            };

            rectangle.Fill = colorBrush;
            _canvas.Children.Add(rectangle);

            Canvas.SetLeft(rectangle, 0);
            Canvas.SetTop(rectangle, 0);
        }

    }

    
}
