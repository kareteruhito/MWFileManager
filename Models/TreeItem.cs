using System.Collections.ObjectModel;
using System.Windows.Media;

namespace MWFileManager;

public class TreeItem
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public ImageSource? Icon { get; set; }

    public ObservableCollection<TreeItem> Children { get; set; } = [];
}