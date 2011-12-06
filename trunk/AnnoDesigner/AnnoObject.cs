using System;
using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner
{
    [Serializable]
    public class AnnoObject
    {
        public Size Size;
        public Color Color;
        public Point Position;

        public string Label;

        public AnnoObject()
        {
        }

        public AnnoObject(AnnoObject obj)
        {
            Size = obj.Size;
            Color = obj.Color;
            Position = obj.Position;
            Label = obj.Label;
        }
    }
}