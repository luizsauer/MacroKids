using System;
using System.Windows.Media;

namespace MacroKids.UI.ViewModels;

public sealed class LanguageOption
{
    public string Name { get; }
    public string Code { get; }
    public ImageSource Icon { get; }

    public LanguageOption(string name, string code, string iconPath)
    {
        Name = name;
        Code = code;
        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath, UriKind.Absolute));
    }
}
