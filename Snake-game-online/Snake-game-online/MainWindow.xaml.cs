using Serilog;
using Snake_game_online;
using SnakeOnline.Game.States;
using System.ComponentModel;
using System.Windows;

namespace SnakeOnline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public interface IOngoingGameInfo
        {
            string Name { get; }

            List<IPlayerState> Players {  get; }

            IGameInfo.IGameConfig GameConfig { get; }

            bool CanJoin {  get; }
        }

        private readonly Presenter _presenter;

        public MainWindow()
        {
            InitializeComponent();

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File("logs.txt", rollingInterval : RollingInterval.Day).CreateLogger();

            Log.Debug("Logger started.");

            Closing += OnWindowClose;
            PlayerNameTextBox.Text = GeneratePlayerName();
            _presenter = new Presenter();
            _presenter.AttachMainWindow(this);
            _presenter.GameListUpdated += (actualList) =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    UpdateGameList(actualList);
                }));
            };
        }

        private void OnWindowClose(object? sender, CancelEventArgs e)
        {
            _presenter.ExitApplication();
        }

        private void UpdateGameList(List<IOngoingGameInfo> actualList)
        {
            OngoingGamesListView.Items.Clear();
            foreach (var gameInfo in actualList)
            {
                OngoingGamesListView.Items.Add(gameInfo);
            }
        }

        private void CreateGameButton_Click(object sender, RoutedEventArgs e)
        {
            string playerName = PlayerNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(playerName)) 
            {
                playerName = GeneratePlayerName();
            }
            GameCreationWindow gameCreationWindow = new GameCreationWindow(_presenter, playerName);
            gameCreationWindow.Show();
        }

        private string GeneratePlayerName()
        {
            return $"Player#{Guid.NewGuid()}";
        }

        private void JoinGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (OngoingGamesListView.SelectedItems.Count == 0)
            {
                DisplayError("Please select a game first.");
                return;
            }
            IOngoingGameInfo? selectedGame = (IOngoingGameInfo?)OngoingGamesListView.SelectedItems[0];
            _presenter.JoinGame(selectedGame, PlayerNameTextBox.Text);
        }

        public void OnGameJoined(IGameInfo.IGameConfig gameConfig)
        {
            Dispatcher.Invoke(() => 
            {
                GameWindow gameWindow = new GameWindow(_presenter,
                new Tuple<int, int>(gameConfig.FieldWidth, gameConfig.FieldHeight));
                gameWindow.Show();
            });
        }

        public void DisplayError(string v)
        {
            Dispatcher.Invoke(() => new ErrorPopup(v).Show());
        }
    }
}