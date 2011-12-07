using System;
using System.Runtime.Serialization;
using System.Windows;

namespace AnnoDesigner
{
    /// <summary>
    /// Object that contains all information needed to fully describe a building on the grid
    /// </summary>
    [DataContract]
    public class AnnoObject
    {
        /// <summary>
        /// Size of in grid units
        /// </summary>
        [DataMember]
        public Size Size;
        
        /// <summary>
        /// Color used to fill this object
        /// </summary>
        [DataMember]
        public SerializableColor Color;
        
        /// <summary>
        /// Position in grid units
        /// </summary>
        [DataMember]
        public Point Position;
        
        /// <summary>
        /// Filename for an icon
        /// </summary>
        [DataMember]
        public string Icon;
        
        /// <summary>
        /// Label string
        /// </summary>
        [DataMember]
        public string Label;
        
        /// <summary>
        /// Influence radius in grid units
        /// </summary>
        [DataMember]
        public int Radius;

        /// <summary>
        /// Empty constructor needed for deserialization
        /// </summary>
        public AnnoObject()
        {
        }

        /// <summary>
        /// Copy constructor used to place independent
        /// </summary>
        /// <param name="obj"></param>
        public AnnoObject(AnnoObject obj)
        {
            Size = obj.Size;
            Color = obj.Color;
            Position = obj.Position;
            Label = obj.Label;
            Icon = obj.Icon;
            Radius = obj.Radius;
        }
    }
}