using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace CopyFilesWPF.Model
{
    /// <summary>
    /// Копіює файли з підтримкою прогресу, паузи та скасування.
    /// </summary>
    public class FileCopier
    {
        private readonly FilePath _filePath;
        private readonly CancellationToken _cancellationToken;
        private readonly ManualResetEventSlim _pauseEvent = new(true);

        /// <summary>
        /// Подія, яка викликається при зміні прогресу копіювання (від 0 до 100).
        /// </summary>
        public event Action<double> ProgressChanged;

        /// <summary>
        /// Подія, яка викликається після завершення копіювання.
        /// </summary>
        public event Action CopyCompleted;

        public FileCopier(FilePath filePath, CancellationToken cancellationToken)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _cancellationToken = cancellationToken;
        }

        public void Pause() => _pauseEvent.Reset();
        public void Resume() => _pauseEvent.Set();

        public void CopyFile()
        {
            const int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize];

            while (true)
            {
                try
                {
                    using var source = new FileStream(_filePath.PathFrom, FileMode.Open, FileAccess.Read);
                    using var destination = new FileStream(_filePath.PathTo, FileMode.CreateNew, FileAccess.Write);

                    long totalBytes = 0;
                    long fileLength = source.Length;

                    int bytesRead;
                    while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        _pauseEvent.Wait(_cancellationToken);

                        destination.Write(buffer, 0, bytesRead);
                        totalBytes += bytesRead;

                        double progress = totalBytes * 100.0 / fileLength;
                        ProgressChanged?.Invoke(progress);
                    }

                    break; // success
                }
                catch (OperationCanceledException)
                {
                    TryDeleteFile(_filePath.PathTo);
                    MessageBox.Show("Copying was canceled!", "Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }
                catch (IOException ex)
                {
                    var result = MessageBox.Show(
                        $"{ex.Message}\n\nFile already exists. Replace it?",
                        "File Exists",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        TryDeleteFile(_filePath.PathTo);
                        continue;
                    }

                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
            }

            CopyCompleted?.Invoke();
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete file: {ex.Message}", "Cleanup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}