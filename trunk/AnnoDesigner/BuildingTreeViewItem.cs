using System.Windows.Controls;

namespace AnnoDesigner
{
    /// <summary>
    /// Represents one item within a TreeView and can be linked with an AnnoObject.
    /// </summary>
    public class BuildingTreeViewItem
        : TreeViewItem
    {
        /// <summary>
        /// AnnoObject linked to this item.
        /// </summary>
        public readonly AnnoObject Object;

        public BuildingTreeViewItem(string header)
        {
            Header = header;
        }

        public BuildingTreeViewItem(string header, AnnoObject obj)
            : this(header)
        {
            Object = obj;
        }

        /// <summary>
        /// Determines whether this item has an AnnoObject linked to it or not.
        /// </summary>
        /// <returns>true if the Object property is not null, otherwise false.</returns>
        public bool IsBuildingItem()
        {
            return Object != null;
        }
    }
}