using System.Windows.Controls;

namespace AnnoDesigner
{
    public class BuildingTreeViewItem
        : TreeViewItem
    {
        public readonly BuildingInfo BuildingInfo;

        public BuildingTreeViewItem(string header)
        {
            BuildingInfo = null;
            Header = header;
        }

        public BuildingTreeViewItem(BuildingInfo buildingInfo)
        {
            BuildingInfo = buildingInfo;
            Header = buildingInfo.Eng;
        }

        public bool IsBuildingItem()
        {
            return BuildingInfo != null;
        }
    }
}