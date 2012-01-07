using System.Windows.Media.Imaging;

namespace AnnoDesigner.UI
{
    public class IconImage
    {
        public string Name
        {
            get;
            private set;
        }

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

        public IconImage(string name)
        {
            Name = name;
            DisplayName = name;
        }

        public IconImage(string name, string displayName, BitmapImage icon)
        {
            Name = name;
            DisplayName = displayName;
            Icon = icon;
        }
    }
}