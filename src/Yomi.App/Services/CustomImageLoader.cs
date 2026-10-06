using System.IO;
using System.Windows.Media.Imaging;

namespace Yomi.App.Services;

public static class CustomImageLoader
{
    public static BitmapSource? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            // 読み込み後にファイルを解放し、元画像の差し替えや削除を妨げない。
            using var stream = File.OpenRead(path);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 552;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }
}
