using System;
using System.IO;
using System.Windows.Media;

namespace MacroKids.UI.ViewModels;

public sealed class LanguageOption
{
    public string Name { get; }
    public string Code { get; }
    public ImageSource Icon { get; }

    public LanguageOption(string name, string code, string iconFileName)
    {
        Name = name;
        Code = code;
        Icon = LoadIcon(iconFileName);
    }

    private static ImageSource? LoadIcon(string iconFileName)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", iconFileName);
            if (!File.Exists(path))
                return null;

            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
