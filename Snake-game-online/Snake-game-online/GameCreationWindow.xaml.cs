using SnakeGameOnline.Model;
using System.Windows;

namespace SnakeGameOnline
{
    /// <summary>
    /// Логика взаимодействия для GameCreationWindow.xaml
    /// </summary>
    public partial class GameCreationWindow : Window
    {

        public const string s_DefaultGameName = "My Game";

        private Presenter _presenter;

        public record GameConfig(string GameName, string PlayerName, 
            int FieldWidth, int FieldHeight, int FoodStatic, int StateDelay_ms) : IGameInfo.IGameConfig;

        public GameCreationWindow(Presenter presenter, string playerName)
        {
            InitializeComponent();
            _presenter = presenter;
            PlayerNameTextBox.Text = playerName;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInputValid())
            {
                ShowError("Input data is not valid.");
            }
            GameConfig gameConfig = GetGameConfig();
            _presenter.StartNewGame(gameConfig);
            GameWindow gameWindow = new GameWindow(_presenter, 
                new Tuple<int, int>(gameConfig.FieldWidth, gameConfig.FieldHeight));
            gameWindow.Show();
            Close();
        }

        private GameConfig GetGameConfig()
        {
            string gameName = GameNameTextBox.Text;
            string playerName = PlayerNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(gameName)) 
            {
                gameName = s_DefaultGameName;
            }
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = GeneratePlayerName();
            }
            return new GameConfig(gameName, playerName, int.Parse(FieldWidthTextBox.Text), int.Parse(FieldHeightTextBox.Text), 
                int.Parse(FoodStaticTextBox.Text), int.Parse(StateDelayTextBox.Text));
        }

        private string GeneratePlayerName()
        {
            return $"Player#{Guid.NewGuid()}";
        }

        private void ShowError(string v)
        {
            Dispatcher.Invoke(() => new ErrorPopup(v).Show());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool IsInputValid()
        {
            return true;
        }
    }
}
