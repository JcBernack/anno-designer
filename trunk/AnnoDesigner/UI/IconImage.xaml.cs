using System.Windows.Media.Imaging;

namespace AnnoDesigner.UI
{
    public class IconImage
    {
        public string DisplayName
        {
            get;
            private set;
        }

        public BitmapImage Icon
        {
            get;
            private set;
        }

        public IconImage(string displayName, BitmapImage icon)
        {
            DisplayName = displayName;
            Icon = icon;
        }
    }
}