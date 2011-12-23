using System.Windows.Controls;

namespace AnnoDesigner
{
    public class IconComboBoxItem
        : ComboBoxItem
    {
        public readonly string IconName;

        public IconComboBoxItem(string iconName)
        {
            IconName = iconName;
            Content = iconName;
        }
    }
}