using System.Runtime.Serialization;
using System.Windows;

namespace AnnoDesigner.Presets
{
    /// <summary>
    /// Contains information for one building type.
    /// Is deserialized from presets.json.
    /// </summary>
    [DataContract]
    public class BuildingInfo
    {
        // technical information
        //[DataMember(Name = "GUID")]
        //public int Guid;
        //[DataMember(Name = ".ifo")]
        //public int IfoFile;

        // main
        [DataMember(Name = "BuildBlocker.x")]
        public int Width;
        [DataMember(Name = "BuildBlocker.z")]
        public int Height;
        [DataMember]
        public string Identifier;
        [DataMember]
        public string IconFileName;
        [DataMember(Name = "Eng1")]
        public string Eng;
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
        //[DataMember(Name = "Production.Product.GUID")]
        //public int ProductGUID;
        //[DataMember(Name = "Production.Product.Name")]
        //public string ProductName;
        //[DataMember(Name = "Production.Product.Eng1")]
        //public string ProductEng1;

        public AnnoObject ToAnnoObject()
        {
            return new AnnoObject
            {
                Label = Eng,
                Icon = IconFileName,
                Radius = InfluenceRadius,
                Size = new Size(Width, Height)
            };
        }

        public string GetOrderParameter()
        {
            return Eng;
        }
    }
}
