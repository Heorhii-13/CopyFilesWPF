using CopyFilesWPF.Presenter;
using CopyFilesWPF.View;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace CopyFilesWPF
{
    public partial class MainWindow : Window, IMainWindowView
    {
        private readonly IMainWindowPresenter _mainWindowPresenter;
        public MainWindow MainWindowView => this;

        public MainWindow()
        {
            InitializeComponent();
            _mainWindowPresenter = new MainWindowPresenter(this);
        }

        private void FromButton_Click(object sender, RoutedEventArgs e)
        {
            FromTextBox.Text = OpenFile();
            _mainWindowPresenter.ChooseFileFromButtonClick(FromTextBox.Text);
        }

        private void ToButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog(); // for this type - please turn on WinForms in project (in properties)
            DialogResult result = dialog.ShowDialog();
            ToTextBox.Text = dialog.SelectedPath;
            _mainWindowPresenter.ChooseFileToButtonClick(ToTextBox.Text);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindowPresenter.CopyButtonClick();
        }

        private void FromTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CopyButton.IsEnabled = CheckFromAndToPaths();
        }

        private void ToTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CopyButton.IsEnabled = CheckFromAndToPaths();
        }

        private bool CheckFromAndToPaths()
        {
            return ToTextBox.Text.Length > 0 && FromTextBox.Text.Length > 0;
        }

        private static string OpenFile()
        {
            var openFile = new OpenFileDialog
            {
                Multiselect = false
            };
            openFile.ShowDialog();

            return openFile.FileName;
        }
    }
}
