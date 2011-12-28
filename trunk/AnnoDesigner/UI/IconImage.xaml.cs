using System.Windows.Media.Imaging;

namespace AnnoDesigner.UI
{
    public class IconImage
    {
        public readonly string DisplayName;
        public readonly BitmapImage Icon;

        public IconImage(string displayName, BitmapImage icon)
        {
            DisplayName = displayName;
            Icon = icon;
        }
    }
}