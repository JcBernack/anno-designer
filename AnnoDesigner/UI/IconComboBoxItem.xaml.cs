namespace AnnoDesigner.UI
{
    /// <summary>
    /// Represents one item within a ComboBox and is linked with an icon name.
    /// </summary>
    public class IconComboBoxItem
    {
        /// <summary>
        /// Name of the icon that this item is linked to.
        /// </summary>
        public readonly string IconName;

        /// <summary>
        /// IconImage to display for this item.
        /// </summary>
        public IconImage Icon
        {
            get;
            private set;
        }

        /// <summary>
        /// String to display next to the icon.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return Icon != null ? Icon.DisplayName : IconName;
            }
        }

        public IconComboBoxItem(string iconName)
        {
            IconName = iconName;
        }

        public IconComboBoxItem(string iconName, IconImage icon)
            : this(iconName)
        {
            Icon = icon;
        }
    }
}