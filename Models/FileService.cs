using System.Diagnostics;
using System.IO;
using Maywork.WPF.Helpers;

namespace MWFileManager;
public static class FileService
{
    // ファイルの一覧を取得
    public static IEnumerable<FileItem> GetFlieList(string path)
    {
        if (File.Exists(path))
            throw new ArgumentException($"{path} is file");
        if (!Directory.Exists(path))
            throw new ArgumentException($"{path} not exists");
        
        return Directory.EnumerateFileSystemEntries(path)
            .Select(file =>
            {
                try
                {
                    var attr = File.GetAttributes(file);
                    return (file, attr); // ← ValueTupleでOK
                }
                catch
                {
                    return ((string file, FileAttributes attr)?)null;
                }
            })
            .Where(x => x.HasValue &&
                (x.Value.attr & (FileAttributes.Hidden | FileAttributes.System)) == 0)
            .Select(x =>
            {
                var (file, attr) = x!.Value;

                return new FileItem()
                {
                    Path = file,
                    DisplayName = Path.GetFileName(file),
                    Icon = IconHelper.GetIconImageSource(file),

                    IsDirectory = (attr & FileAttributes.Directory) != 0
                };
            })
            .ToList();
    }

    // ドライブの一覧を取得
    public static IEnumerable<TreeItem> GetDriveList()
    {

        return DriveInfo.GetDrives()
            .Select(drive =>
            {
                return new TreeItem()
                {
                    Path = drive.Name,
                    Name = drive.Name,
                    Icon = IconHelper.GetIconImageSource(drive.Name),
                    Children = [ new TreeItem() ], // ダミーTreeItem
                };                
            })
            .ToList();
    }
    // ディレクトリの一覧を取得
    public static IEnumerable<TreeItem> GetDirectroyList(string path)
    {
        if (File.Exists(path))
            throw new ArgumentException($"{path} is file");
        if (!Directory.Exists(path))
            throw new ArgumentException($"{path} not exists");
        
        return Directory.EnumerateDirectories(path)
            .Select(dir =>
            {
                try
                {
                    var attr = File.GetAttributes(dir);
                    return (dir, attr); // ← ValueTupleでOK
                }
                catch
                {
                    return ((string dir, FileAttributes attr)?)null;
                }
            })
            .Where(x => x.HasValue &&
                (x.Value.attr & (FileAttributes.Hidden | FileAttributes.System)) == 0)
            .Select(x =>
            {
                var (dir, attr) = x!.Value;

                return new TreeItem()
                {
                    Path = dir,
                    Name = Path.GetFileName(dir),
                    Icon = IconHelper.GetIconImageSource(dir),
                    Children = [ new TreeItem() ], // ダミーTreeItem
                };
            })
            .ToList();       
    }
    // ドキュメントフォルダのパスを取得
    public static string GetDocumentPath()
        => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    // ディレクトリ判定
    public static bool IsDirectory(string path)
        => Directory.Exists(path);
    // 親ディレクトリのパスを取得
    public static string GetParentDir(string path)
        => Path.GetDirectoryName(path) ?? "";

    public static void CopyWithNumbering(string sourcePath, string destDirectory)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("コピー元が存在しません", sourcePath);

        string fileName = Path.GetFileNameWithoutExtension(sourcePath);
        string extension = Path.GetExtension(sourcePath);

        string destPath = Path.Combine(destDirectory, fileName + extension);

        int count = 1;

        // 同名ファイルが存在する場合、(1),(2)... を付ける
        while (File.Exists(destPath))
        {
            string newFileName = $"{fileName}({count}){extension}";
            destPath = Path.Combine(destDirectory, newFileName);
            count++;
        }

        File.Copy(sourcePath, destPath);
    }

    public static void DeleteFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("パスが不正です", nameof(path));

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static void MoveWithNumbering(string sourcePath, string destDirectory)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("移動元が存在しません", sourcePath);

        if (!Directory.Exists(destDirectory))
            Directory.CreateDirectory(destDirectory);

        string fileName = Path.GetFileNameWithoutExtension(sourcePath);
        string extension = Path.GetExtension(sourcePath);

        string destPath = Path.Combine(destDirectory, fileName + extension);

        int count = 1;

        // 同名ファイルが存在する場合、(1),(2)... を付ける
        while (File.Exists(destPath))
        {
            string newFileName = $"{fileName}({count}){extension}";
            destPath = Path.Combine(destDirectory, newFileName);
            count++;
        }

        File.Move(sourcePath, destPath);
    }
    public static void OpenFile(string filePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }
}