using System.Windows;

namespace SnakeGameOnline
{
    /// <summary>
    /// Логика взаимодействия для ErrorPopup.xaml
    /// </summary>
    public partial class ErrorPopup : Window
    {
        public ErrorPopup(string errorDescription)
        {
            InitializeComponent();
            ErrorDescription.Content = errorDescription;
        }
    }
}
