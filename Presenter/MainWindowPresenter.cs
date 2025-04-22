using CopyFilesWPF.Model;
using CopyFilesWPF.View;
using System;
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

        public MainWindowPresenter(IMainWindowView mainWindowView)
        {
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

        public void CopyButtonClick()
        {
            string pathFrom = _mainWindowView.MainWindowView.FromTextBox.Text;
            string pathTo = _mainWindowView.MainWindowView.ToTextBox.Text;

            if (string.IsNullOrWhiteSpace(pathFrom) || string.IsNullOrWhiteSpace(pathTo))
                return;

            _mainWindowModel.FilePath.PathFrom = pathFrom;
            _mainWindowModel.FilePath.PathTo = pathTo;

            ClearPathInputs();
            AddFileCopyUI(Path.GetFileName(pathFrom), out Grid copyPanel, out ProgressBar progressBar, out Button pauseResumeButton, out Button cancelButton);

            var cts = new CancellationTokenSource();
            var fileCopier = new FileCopier(cts.Token);
            copyPanel.Tag = fileCopier;

            pauseResumeButton.Click += (s, e) => TogglePause(fileCopier, pauseResumeButton);
            cancelButton.Click += (s, e) => CancelCopy(fileCopier, cancelButton);

            _mainWindowModel.CopyFile(
                (percent, ref bool _) => UpdateProgressUI(copyPanel, percent),
                () => OnCopyComplete(copyPanel),
                fileCopier
            );
        }

        private void ClearPathInputs()
        {
            _mainWindowView.MainWindowView.FromTextBox.Text = string.Empty;
            _mainWindowView.MainWindowView.ToTextBox.Text = string.Empty;
        }

        private void AddFileCopyUI(string fileName, out Grid panel, out ProgressBar progressBar, out Button pauseButton, out Button cancelButton)
        {
            _mainWindowView.MainWindowView.Height += 60;

            panel = new Grid
            {
                Height = 60
            };

            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
            panel.ColumnDefinitions.Add(new ColumnDefinition());
            panel.ColumnDefinitions.Add(new ColumnDefinition());
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            panel.RowDefinitions.Add(new RowDefinition());

            var fileNameBlock = new TextBlock
            {
                Text = fileName,
                Margin = new Thickness(5, 0, 5, 0)
            };
            Grid.SetRow(fileNameBlock, 0);
            Grid.SetColumn(fileNameBlock, 0);
            panel.Children.Add(fileNameBlock);

            progressBar = new ProgressBar
            {
                Margin = new Thickness(10)
            };
            Grid.SetRow(progressBar, 1);
            Grid.SetColumn(progressBar, 0);
            panel.Children.Add(progressBar);

            pauseButton = new Button
            {
                Content = "Pause",
                Margin = new Thickness(5)
            };
            Grid.SetRow(pauseButton, 1);
            Grid.SetColumn(pauseButton, 1);
            panel.Children.Add(pauseButton);

            cancelButton = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(5)
            };
            Grid.SetRow(cancelButton, 1);
            Grid.SetColumn(cancelButton, 2);
            panel.Children.Add(cancelButton);

            DockPanel.SetDock(panel, Dock.Top);
            _mainWindowView.MainWindowView.MainPanel.Children.Add(panel);
        }

        private void TogglePause(FileCopier fileCopier, Button pauseButton)
        {
            pauseButton.IsEnabled = false;

            if (pauseButton.Content.ToString() == "Pause")
            {
                fileCopier.Pause();
                pauseButton.Content = "Resume";
            }
            else
            {
                fileCopier.Resume();
                pauseButton.Content = "Pause";
            }

            pauseButton.IsEnabled = true;
        }

        private void CancelCopy(FileCopier fileCopier, Button cancelButton)
        {
            cancelButton.IsEnabled = false;
            fileCopier.Cancel();
        }

        private void UpdateProgressUI(Grid panel, double percentage)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(() =>
            {
                foreach (var element in panel.Children)
                {
                    if (element is ProgressBar bar)
                    {
                        bar.Value = percentage;
                    }
                }
            }));
        }

        private void OnCopyComplete(Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(() =>
            {
                _mainWindowView.MainWindowView.Height -= 60;
                _mainWindowView.MainWindowView.MainPanel.Children.Remove(panel);
                _mainWindowView.MainWindowView.CopyButton.IsEnabled = true;
            }));
        }
    }

    // Модель копирования с CancellationToken
    public class FileCopier
    {
        private readonly CancellationToken _token;
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        public bool IsCanceled { get; private set; }

        public FileCopier(CancellationToken token)
        {
            _token = token;
        }

        public void Pause()
        {
            _pauseEvent.Reset();
        }

        public void Resume()
        {
            _pauseEvent.Set();
        }

        public void Cancel()
        {
            IsCanceled = true;
        }

        public void WaitIfPaused()
        {
            _pauseEvent.Wait();
        }

        public bool IsCancellationRequested => _token.IsCancellationRequested || IsCanceled;
    }
}