using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CopyFilesWPF.Model
{
    public class FileCopier
    {
        private readonly Grid _gridPanel;
        private readonly FilePath _filePath;

        public delegate void ProgressChangeDelegate(double progress, ref bool cancel, Grid gridPanel);
        public delegate void CompleteDelegate(Grid gridPanel);
        public event ProgressChangeDelegate OnProgressChanged;
        public event CompleteDelegate OnComplete;

        public bool CancelFlag = false;
        public ManualResetEvent PauseFlag = new(true);

        public FileCopier(
            FilePath filePath,
            ProgressChangeDelegate onProgressChange,
            CompleteDelegate onComplete,
            Grid gridPanel)
        {
            OnProgressChanged += onProgressChange;
            OnComplete += onComplete;
            _filePath = filePath;
            _gridPanel = gridPanel;
        }

        public void CopyFile()
        {
            byte[] buffer = new byte[1024 * 1024];
            bool isCopy = true; // переделать решение на использование CancellationToken

            while (isCopy)
            {
                try
                {
                    using(var source = new FileStream(_filePath.PathFrom, FileMode.Open, FileAccess.Read))
                    {
                        var fileLength = source.Length;
                        using var destination = new FileStream(_filePath.PathTo, FileMode.CreateNew, FileAccess.Write);
                        long totalBytes = 0;
                        int currentBlockSize = 0;
                        while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytes += currentBlockSize;
                            double persentage = totalBytes * 100.0 / fileLength;
                            destination.Write(buffer, 0, currentBlockSize);
                            OnProgressChanged(persentage, ref CancelFlag, _gridPanel);

                            if(CancelFlag == true)
                            {
                                File.Delete(_filePath.PathTo);
                                isCopy = false;
                                break;
                            }

                            CancelFlag = false; // переделать решение на использование CancellationToken
                            PauseFlag.WaitOne(Timeout.Infinite); // переделать на thread suspend
                        }
                    }
                    isCopy = false;
                }
                catch (IOException error)
                {
                    // порефакторить код ниже
                    if (!CancelFlag)
                    {
                        var result = MessageBox.Show(error.Message + " Replace?", "Replace?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        isCopy = result == MessageBoxResult.Yes;

                        if (isCopy)
                        {
                            File.Delete(_filePath.PathTo);
                        }
                    }
                    else
                    {
                        var result = MessageBox.Show(error.Message + " Copying was canceled!", "Cancel", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        isCopy = false;
                        File.Delete(_filePath.PathTo);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Error occured!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            OnComplete(_gridPanel);
        }
    }
}
