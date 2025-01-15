using Snake_game_online.model.Game.GameState;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SnakeGameOnline
{
    /// <summary>
    /// Логика взаимодействия для GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {

        private readonly Presenter _presenter;
        private readonly CanvasDrawer _drawer;

        public GameWindow(Presenter presenter, Tuple<int, int> fieldSize)
        {
            InitializeComponent();
            KeyDown += GameWindow_KeyDown;
            Closing += OnWindowClose;
            _presenter = presenter;
            _drawer = new CanvasDrawer(FieldCanvas, fieldSize);
            _presenter.GameStateUpdated += OnGameStateUpdate;
        }
        
        private void OnGameStateUpdate(object sender, EventArgs eventArgs)
        {
            IGameState state = ((Presenter.GameStateUpdatedEventArgs)eventArgs).GameState;
            Dispatcher.Invoke(() =>
            {
                DisplayGameState(state);
            });
        }

        private void DisplayGameState(IGameState state)
        {
            _drawer.DrawField(state.GetFieldState());
            ShowPlayers(state.GetIPlayerState());
        }

        private void ShowPlayers(List<IPlayerState> playerStates)
        {
            Leaderboard.Items.Clear();
            foreach (var state in playerStates)
            {
                Leaderboard.Items.Add($"{state.GetName()} : {state.GetScore()}");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowClose(object? sender, CancelEventArgs e)
        {
            _presenter.GameStateUpdated -= OnGameStateUpdate;
            _presenter.ExitGame();
        }
 
        private void GameWindow_KeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.Key)
            {
                case Key.A:
                case Key.Left:
                    _presenter.ChangeMyPlayerDirection(ISnakeState.Direction.LEFT);
                    break;
                case Key.D:
                case Key.Right:
                    _presenter.ChangeMyPlayerDirection(ISnakeState.Direction.RIGHT);
                    break;
                case Key.W:
                case Key.Up:
                    _presenter.ChangeMyPlayerDirection(ISnakeState.Direction.UP);
                    break;
                case Key.S:
                case Key.Down:
                    _presenter.ChangeMyPlayerDirection(ISnakeState.Direction.DOWN);
                    break;
            }
        }
    }
}
