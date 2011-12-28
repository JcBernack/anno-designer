using System.Windows.Controls;

namespace AnnoDesigner.UI
{
    /// <summary>
    /// Represents one item within a ComboBox and is linked with an icon name.
    /// </summary>
    public class IconComboBoxItem
        : ComboBoxItem
    {
        /// <summary>
        /// Name of the icon that this item is linked to.
        /// </summary>
        public readonly string IconName;

        public IconComboBoxItem(string displayName)
        {
            Content = displayName;
        }

        public IconComboBoxItem(string displayName, string iconName)
            : this(displayName)
        {
            IconName = iconName;
        }
    }
}