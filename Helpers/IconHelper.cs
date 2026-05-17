using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Maywork.WPF.Helpers;
public static class IconHelper
{
    // ------------------------------------------------------------
    // Win32 API
    // ------------------------------------------------------------

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("Shell32.dll", EntryPoint = "#727")]
    private static extern int SHGetImageList(
        int iImageList,
        ref Guid riid,
        out IImageList ppv);

    [DllImport("User32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    // ------------------------------------------------------------
    // Structs & Constants
    // ------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_SYSICONINDEX = 0x00004000;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    private const int SHIL_SMALL = 0;       // 16x16
    private const int SHIL_LARGE = 1;       // 32x32
    private const int SHIL_EXTRALARGE = 2;  // 48x48
    private const int SHIL_JUMBO = 4;       // 256x256

    // ------------------------------------------------------------
    // COM Interface (Order is critical!)
    // ------------------------------------------------------------

    [ComImport]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IImageList
    {
        int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
        int ReplaceIcon(int i, IntPtr hicon, ref int pi);
        int SetOverlayImage(int iImage, int iOverlay);
        int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
        int AddMasked(IntPtr hbmImage, uint crMask, ref int pi);
        int Draw(IntPtr pimldp); // 引数は簡易化
        int Remove(int i);
        int GetIcon(int i, int flags, out IntPtr picon); // 8番目のメソッド
        // 以降のメソッドは今回使用しないため省略可能
    }

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    public static ImageSource GetIconImageSource(string path, int size = 32)
    {
        var info = new SHFILEINFO();

        bool isDirectory = System.IO.Directory.Exists(path);

        uint attr = isDirectory ? (uint)0x10 : (uint)0x80; // 0x10: Directory, 0x80: Normal File
    
        uint flags = SHGFI_SYSICONINDEX | SHGFI_USEFILEATTRIBUTES;
    
        // 第一引数に path を渡しても、USEFILEATTRIBUTES があれば属性優先になります。
        SHGetFileInfo(path, attr, out info, (uint)Marshal.SizeOf(info), flags);

        // 2. サイズに応じたイメージリストの種類を選択
        int listType = size switch
        {
            <= 16 => SHIL_SMALL,
            <= 32 => SHIL_LARGE,
            <= 48 => SHIL_EXTRALARGE,
            _ => SHIL_JUMBO
        };

        // 3. IImageList 取得
        Guid guid = typeof(IImageList).GUID;
        int hr = SHGetImageList(listType, ref guid, out var imageList);
        

        if (hr == 0 && imageList != null)
        {
            hr = imageList.GetIcon(info.iIcon, 0x00000001, out IntPtr hIcon);
            if (hr == 0 && hIcon != IntPtr.Zero)
            {
                try
                {
                // BitmapSizeOptions を指定せず、元のサイズを維持して作成
                    var bmp = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()); 

                    bmp.Freeze();
                    return bmp;
                }
                finally
                {
                    DestroyIcon(hIcon); // ハンドル漏れ防止
                }
            }
        }



        // 何も取れなければ透明1x1
        var wb = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
        wb.Freeze();
        return wb;
    }

}