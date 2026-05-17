using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MWFileManager;


public partial class MainWindow : Window
{
    // フォルダツリーノード
    public ObservableCollection<TreeItem> Nodes { get; } = [];
    // ファイルリストアイテム
    public ObservableCollection<FileItem> Files { get; set; } = [];
    // カレントディレクトリ
    public string CurrentDir = FileService.GetDocumentPath();
    // コンストラクタ
    public MainWindow()
    {
        InitializeComponent();


        TreeView_Initialize();
        ListView_MovePath(CurrentDir);

        DataContext = this;

        this.Loaded += (s, e) => Name_Header_Click(s, e); 
    }
    // ツリービューの初期化
    private void TreeView_Initialize()
    {
        foreach (var drive in FileService.GetDriveList())
            Nodes.Add(drive);
        DriveTreeView.ItemsSource = Nodes;        
    }

    // ツリービューの選択中アイテムの変更イベントハンドラ
    private void TreeView_SelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        // 新しい値がTreeItemか確認
        if (e.NewValue is TreeItem node)
        {
            // 選択アイテムのパスでカレントディレクトリの移動
            if (this.CurrentDir != node.Path)
                ListView_MovePath(node.Path);
        }
    }
    // ツリービューアイテムの展開イベントハンドラ
    private void TreeViewItem_Expanded(
        object sender,
        RoutedEventArgs e)
    {

        if (e.OriginalSource is not TreeViewItem item ||
            item.DataContext is not TreeItem node)
        {
            return;
        }

        if (node.Children.Count == 0) return;
        if (node.Children[0].Path != "") return;
        
        node.Children.Clear();
        node.Children.Add(new TreeItem
        {
            Name = "読み込み中...",
            Path = ""
        });

        node.Children.Clear();

        foreach (var dir in FileService.GetDirectroyList(node.Path))
            node.Children.Add(dir);
    }
    // リストビューのパスの移動
    private void ListView_MovePath(string path)
    {
        Files.Clear();

        foreach (var file in FileService.GetFlieList(path))
            Files.Add(file);
        
        this.CurrentDir = path;
        if (AddressBar.Text != this.CurrentDir)
            AddressBar.Text = this.CurrentDir;
        
    }
    // リストビューアイテムのダブルクリックイベントハンドラ
    private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item)
        {
            var data = (FileItem)item.DataContext; // ダブルクリックされた行のデータ
            // ここで処理

            if (FileService.IsDirectory(data.Path))
            {
                // ディレクトリ
                if (this.CurrentDir != data.Path)
                    ListView_MovePath(data.Path);
            }
            else
            {
                // ファイル
                FileService.OpenFile(data.Path);
            }
        }
    }
    // 親ディレクトリへ移動ボタンクリックイベントハンドラ
    private void ParentDirButton_Click(object sender, RoutedEventArgs e)
    {
        string parent = FileService.GetParentDir(this.CurrentDir);

        if (parent == "") return;

        ListView_MovePath(parent);
    }
    // アドレスバーの移動ボタンクリックイベントハンドラ
    private void MoveDirButton_Click(object sender, RoutedEventArgs e)
    {
        string dir = AddressBar.Text;

        if (!FileService.IsDirectory(dir))
        {
            AddressBar.Text = this.CurrentDir;
            return;
        }
        if (AddressBar.Text == this.CurrentDir) return;

        ListView_MovePath(dir);
    }
    // アプリの終了
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
    List<string> CopyPaths = [];
    // コピー
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in FileListView.SelectedItems)
        {
            var file = (FileItem)item;
            CopyPaths.Add(file.Path);
        }
        if (CopyPaths.Count > 0)
            Context_MenuItem_Paste.Visibility = Visibility.Visible;
        MovePaths.Clear();
    }
    List<string> MovePaths = [];
    // 移動
    private void Move_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in FileListView.SelectedItems)
        {
            var file = (FileItem)item;
            MovePaths.Add(file.Path);
        }
        if (MovePaths.Count > 0)
            Context_MenuItem_Paste.Visibility = Visibility.Visible;
        CopyPaths.Clear();
    }
    // 貼り付け
    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        foreach (var srcPath in CopyPaths)
        {
            FileService.CopyWithNumbering(srcPath, this.CurrentDir);            
        }
        foreach (var srcPath in MovePaths)
        {
            FileService.MoveWithNumbering(srcPath, this.CurrentDir);            
        }
        ListView_MovePath(this.CurrentDir);
        CopyPaths.Clear();
        MovePaths.Clear();
        Context_MenuItem_Paste.Visibility = Visibility.Collapsed;
    }
   // 削除
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (FileListView.SelectedItems.Count <= 0) return;
        
        var result = MessageBox.Show(
            $"削除しますか？",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        foreach (var item in FileListView.SelectedItems)
        {
            var file = (FileItem)item;
            string deletePath = file.Path;

            FileService.DeleteFile(deletePath);
        }
        ListView_MovePath(this.CurrentDir);
    }
    
    // 並べ替え
    private void Name_Header_Click(object sender, RoutedEventArgs e)
    {
        const string propertyName = "IsDirectory";
        var collectionView = CollectionViewSource.GetDefaultView(Files);
        if (collectionView.SortDescriptions.Any() == false ||
            collectionView.SortDescriptions[0].PropertyName != propertyName)
        {
            collectionView.SortDescriptions.Clear();

            // ディレクトリを先にする
            collectionView.SortDescriptions.Add(
                new SortDescription(nameof(FileItem.IsDirectory),
                                    ListSortDirection.Descending));

            // 名前順
            collectionView.SortDescriptions.Add(
                new SortDescription(nameof(FileItem.DisplayName),
                                    ListSortDirection.Ascending));

            return;
        }
        if (collectionView.SortDescriptions[0].Direction == ListSortDirection.Ascending)
        {
            collectionView.SortDescriptions.Clear();
            // ディレクトリを先にする
            collectionView.SortDescriptions.Add(
                new SortDescription(nameof(FileItem.IsDirectory),
                    ListSortDirection.Descending));
            // 名前順
            collectionView.SortDescriptions.Add(
                new SortDescription(nameof(FileItem.DisplayName), 
                    ListSortDirection.Ascending));            
        }
        else
        {
            collectionView.SortDescriptions.Clear();
            // ディレクトリを先にする
            collectionView.SortDescriptions.Add(
                new SortDescription(nameof(FileItem.IsDirectory),
                    ListSortDirection.Ascending));
            // 名前順
            collectionView.SortDescriptions.Add(
                new SortDescription(nameof(FileItem.DisplayName), 
                    ListSortDirection.Descending));            
        }

    }
}

// dotnet publish MWFileManager.csproj -c Release -r win-x64 --self-contained false -o publish