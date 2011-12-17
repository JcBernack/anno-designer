using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace AnnoDesigner
{
    public class Presets
    {
        public List<BuildingInfo> BuildingInfos
        {
            get;
            private set;
        }

        public Presets()
        {
            BuildingInfos = DataIO.LoadFromFile<List<BuildingInfo>>("presets.json");
        }
    }

    [DataContract]
    public class BuildingInfo
    {
        // main
        [DataMember(Name = "BuildBlocker.x")]
        public int Width;
        [DataMember(Name = "BuildBlocker.z")]
        public int Heigth;
        [DataMember]
        public string Name;
        [DataMember]
        public string IconFileName;
        [DataMember]
        public string Eng1;
        [DataMember]
        public int InfluenceRadius;
        
        // grouping
        [DataMember]
        public string Faction;
        [DataMember]
        public string Group;
        [DataMember]
        public string Template;

        // production
        [DataMember(Name = "Production.Product.GUID")]
        public int ProductGUID;
        [DataMember(Name = "Production.Product.Name")]
        public string ProductName;
        [DataMember(Name = "Production.Product.Eng1")]
        public string ProductEng1;

        // additional
        //[DataMember]
        //public int GUID;

        public AnnoObject ToAnnoObject()
        {
            return new AnnoObject
            {
                Label = Eng1,
                Icon = IconFileName,
                Radius = InfluenceRadius,
                Size = new Size(Width, Heigth)
            };
        }

        public string GetDisplayValue()
        {
            return Name;
        }
    }
}
