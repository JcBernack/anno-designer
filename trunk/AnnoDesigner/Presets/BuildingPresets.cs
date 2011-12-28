using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Controls;
using AnnoDesigner.UI;

namespace AnnoDesigner.Presets
{
    /// <summary>
    /// Notes for buildings list json:
    /// capitalize "buildings" live everything else
    /// some radii are missing, e.g. coffee plantation
    /// </summary>
    [DataContract]
    public class BuildingPresets
    {
        [DataMember(Name = "_model")]
        public string Model;

        [DataMember(Name = "_version")]
        public string Version;

        [DataMember(Name = "buildings")]
        public List<BuildingInfo> Buildings;

        public void AddToTree(TreeView treeView)
        {
            var excludedTemplates = new[] { "Ark", "Harbour", "OrnamentBuilding" };
            var excludedFactions = new[] { "third party" };
            var list = Buildings.Where(_ => !excludedTemplates.Contains(_.Template)).Where(_ => !excludedFactions.Contains(_.Faction));
            foreach (var firstLevel in list.GroupBy(_ => _.Faction).OrderBy(_ => _.Key))
            {
                var firstLevelItem = new BuildingTreeViewItem(firstLevel.Key);
                foreach (var secondLevel in firstLevel.GroupBy(_ => _.Group).OrderBy(_ => _.Key))
                {
                    var secondLevelItem = new BuildingTreeViewItem(secondLevel.Key);
                    foreach (var buildingInfo in secondLevel.OrderBy(_ => _.GetOrderParameter()))
                    {
                        secondLevelItem.Items.Add(new BuildingTreeViewItem(buildingInfo.Eng, buildingInfo.ToAnnoObject()));
                    }
                    firstLevelItem.Items.Add(secondLevelItem);
                }
                treeView.Items.Add(firstLevelItem);
            }
        }
    }
}