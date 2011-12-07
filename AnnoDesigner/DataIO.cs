using System.IO;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnnoDesigner
{
    public static class DataIO
    {
        public static void SaveToFile<T>(T obj, string filename)
        {
            var stream = File.Open(filename, FileMode.Create);
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(stream, obj);
            stream.Close();
        }

        public static T LoadFromFile<T>(string filename)
        {
            T obj;
            LoadFromFile(out obj, filename);
            return obj;
        }

        public static void LoadFromFile<T>(out T obj, string filename)
        {
            var stream = File.Open(filename, FileMode.Open);
            var serializer = new DataContractJsonSerializer(typeof(T));
            obj = (T)serializer.ReadObject(stream);
            stream.Close();
        }

        public static void RenderToFile(FrameworkElement controlToRender, string filename)
        {
            var rtb = new RenderTargetBitmap((int)controlToRender.ActualWidth, (int)controlToRender.ActualHeight, 90, 90, PixelFormats.Default);

            Visual vis = controlToRender;
            rtb.Render(vis);

            var img = new System.Windows.Controls.Image { Source = rtb, Stretch = Stretch.None };
            img.Measure(new Size((int)controlToRender.ActualWidth,(int)controlToRender.ActualHeight));
            var sizeImage = img.DesiredSize;
            img.Arrange(new Rect(new Point(0, 0), sizeImage));

            var rtb2 = new RenderTargetBitmap((int)rtb.Width, (int)rtb.Height, 90, 90, PixelFormats.Default);
            rtb2.Render(img);

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb2));

            Stream file = File.Open(filename, FileMode.Create);
            png.Save(file);
            file.Close();
        }
    }
}