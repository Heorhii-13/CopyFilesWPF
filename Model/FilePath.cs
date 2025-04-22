using System.IO;

namespace CopyFilesWPF.Model
{
    public sealed record FilePath
    {
        private string? pathTo { get; set; }

        public string PathFrom { get; set; } = string.Empty;

        public string PathTo
        {
            get => pathTo + "\\" + Path.GetFileName(PathFrom);
            set => pathTo = value;
        }
    }
}
