using System;
using System.Windows;

namespace AnnoDesigner
{
    [Serializable]
    public class AnnoObject
    {
        public Size Size;
        public SerializableColor Color;
        public Point Position;
        public string Icon;
        public string Label;
        public int Radius;

        public AnnoObject()
        {
        }

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