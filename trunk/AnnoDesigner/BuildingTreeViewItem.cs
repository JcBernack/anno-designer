using System.Windows.Controls;

namespace AnnoDesigner
{
    public class BuildingTreeViewItem
        : TreeViewItem
    {
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

        public bool IsBuildingItem()
        {
            return Object != null;
        }
    }
}