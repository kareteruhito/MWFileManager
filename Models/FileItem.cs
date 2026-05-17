using System.Windows.Media;

namespace MWFileManager;
public class FileItem
{
    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ImageSource? Icon { get; set; }
    public bool IsDirectory { get; set; }
}