namespace CopyFilesWPF.Presenter
{
    public interface IMainWindowPresenter
    {
        void CopyButtonClick();

        void ChooseFileFromButtonClick(string path);

        void ChooseFileToButtonClick(string path);
    }
}
