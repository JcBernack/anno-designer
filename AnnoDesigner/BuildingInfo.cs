using System.Collections.Generic;
using System.Windows;

namespace AnnoDesigner
{
    public class BuildingInfo
    {
        // main info
        public List<int> BuildBlocker;
        public string Name;
        public string IconFileName;
        public string Eng1;
        public int InfluenceRadius;
        
        // additional info
        public string Template;
        public int GUID;
        public int ProductGUID;
        public string ProductName;
        public string ProductEng1;

        public AnnoObject ToAnnoObject()
        {
            return new AnnoObject
            {
                Label = Eng1,
                Icon = IconFileName,
                Radius = InfluenceRadius,
                Size = BuildBlocker != null ? new Size(BuildBlocker[0], BuildBlocker[1]) : new Size(0,0)
            };
        }

        public string GetDisplayValue()
        {
            return Name;
        }
    }
}
