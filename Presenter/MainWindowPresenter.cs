using CopyFilesWPF.Model;
using CopyFilesWPF.View;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CopyFilesWPF.Presenter
{
    public class MainWindowPresenter : IMainWindowPresenter
    {
        private readonly IMainWindowView _mainWindowView;
        private readonly MainWindowModel _mainWindowModel;

        public MainWindowPresenter(IMainWindowView mainWindowView) {
            _mainWindowView = mainWindowView;
            _mainWindowModel = new MainWindowModel();
        }

        public void ChooseFileFromButtonClick(string path)
        {
            _mainWindowModel.FilePath.PathFrom = path;
        }

        public void ChooseFileToButtonClick(string path)
        {
            _mainWindowModel.FilePath.PathTo = path;
        }

        // порефакторить этот метод, убрать хардкод, разделить на более мелкие методы
        public void CopyButtonClick()
        {
            _mainWindowModel.FilePath.PathFrom = _mainWindowView.MainWindowView.FromTextBox.Text;
            _mainWindowModel.FilePath.PathTo = _mainWindowView.MainWindowView.ToTextBox.Text;
            _mainWindowView.MainWindowView.FromTextBox.Text = "";
            _mainWindowView.MainWindowView.ToTextBox.Text = "";
            _mainWindowView.MainWindowView.Height = _mainWindowView.MainWindowView.Height + 60;
            var newPanel = new Grid();
            newPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
            newPanel.ColumnDefinitions.Add(new ColumnDefinition());
            newPanel.ColumnDefinitions.Add(new ColumnDefinition());
            newPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            newPanel.RowDefinitions.Add(new RowDefinition());
            var nameFile = new TextBlock
            {
                Text = Path.GetFileName(_mainWindowModel.FilePath.PathFrom),
                Margin = new Thickness(5, 0, 5, 0)
            };
            Grid.SetRow(nameFile, 0);
            Grid.SetColumn(nameFile, 0);
            newPanel.Children.Add(nameFile);
            var progressBar = new ProgressBar
            {
                Margin = new Thickness(10, 10, 10, 10)
            };
            Grid.SetRow(progressBar, 1);
            newPanel.Children.Add(progressBar);
            var pauseB = new Button
            {
                Content = "Pause",
                Margin = new Thickness(5),
                Tag = newPanel
            };
            pauseB.Click += PauseCancelClick;
            Grid.SetRow(pauseB, 1);
            Grid.SetColumn(pauseB, 1);
            newPanel.Children.Add(pauseB);
            var cancelB = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(5),
                Tag = newPanel
            };
            cancelB.Click += PauseCancelClick;
            Grid.SetRow(cancelB, 1);
            Grid.SetColumn(cancelB, 2);
            newPanel.Children.Add(cancelB);
            DockPanel.SetDock(newPanel, Dock.Top);
            newPanel.Height = 60;
            _mainWindowView.MainWindowView.MainPanel.Children.Add(newPanel);
            _mainWindowModel.CopyFile(ProgressChanged, ModelOnComplete, newPanel);
        }

        // порефакторить этот метод, убрать хардкод, и переделать его по SOLID (тут несколько ответсвенностей)
        private void PauseCancelClick(object sender, RoutedEventArgs routedEventArgs)
        {
            ((Button)sender).IsEnabled = false;
            if(((Button)sender)!.Content.ToString()!.Equals("Cancel")) {
                ((((Button)sender).Tag as Grid)!.Tag as FileCopier)!.CancelFlag = true;
            }
            else if (((Button)sender)!.Content.ToString()!.Equals("Pause"))
            {
                ((((Button)sender).Tag as Grid)!.Tag as FileCopier)!.PauseFlag.Reset();
            }
            else
            {
                ((((Button)sender).Tag as Grid)!.Tag as FileCopier)!.PauseFlag.Set();
            }
        }

        private void ModelOnComplete(Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    _mainWindowView.MainWindowView.Height = _mainWindowView.MainWindowView.Height - 60;
                    _mainWindowView.MainWindowView.MainPanel.Children.Remove(panel);
                    _mainWindowView.MainWindowView.CopyButton.IsEnabled = true;
                }
            );
        }

        // порефакторить этот метод, убрать хардкод, и переделать его по SOLID (тут несколько ответсвенностей)
        private void ProgressChanged(double persentage, ref bool cancelFlag, Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    foreach (var el in panel.Children)
                    {
                        if (el is ProgressBar bar)
                        {
                            bar.Value = persentage;
                        }
                        if (el is Button button1 && button1!.Content.ToString()!.Equals("Resume") && button1!.IsEnabled == false)
                        {
                            button1.Content = "Pause";
                            button1.IsEnabled = true;
                        }
                        else if (el is Button button && button!.Content.ToString()!.Equals("Pause") && button.IsEnabled == false)
                        {
                            button.Content = "Resume";
                            button.IsEnabled = true;
                        }
                    }
                }
            );
        }
    }
}
