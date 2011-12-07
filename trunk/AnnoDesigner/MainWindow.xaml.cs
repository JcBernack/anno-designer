using System.Windows;
using System.Windows.Media;
using Microsoft.Windows.Controls;

namespace AnnoDesigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
        : Window
    {
        private AnnoObject _currentObject;

        public MainWindow()
        {
            InitializeComponent();
            colorPicker.StandardColors.Clear();
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(214, 49, 49), "red"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(171, 232, 107), "green"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(76, 106, 222), "blue"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(247, 150, 70), "orange"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(0, 0, 0), "black"));
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button1Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // parse user inputs and create new object
                _currentObject = new AnnoObject
                {
                    Size = new Size(int.Parse(textBoxWidth.Text), int.Parse(textBoxHeight.Text)),
                    Color = colorPicker.SelectedColor,
                    Label = textBoxLabel.Text,
                    Icon = comboBoxIcon.SelectedIndex > 0 ? @"images\Liquor.png" : null,
                    Radius = int.Parse(textBoxRadius.Text)
                };
                // do some sanity checks
                if (_currentObject.Size.Width > 0 && _currentObject.Size.Height > 0 && _currentObject.Radius >= 0)
                {
                    annoCanvas.SetCurrentObject(_currentObject);
                }
            }
            catch
            {
                
            }
        }

        private void MenuItemNewClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.ClearPlacedObjects();
        }

        private void MenuItemSaveAsClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.SaveToFile();
        }

        private void MenuItemOpenClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.OpenFile();
        }

        private void MenuItemExportImageClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.ExportImage();
        }

        private void MenuItemGridClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.RenderGrid = !annoCanvas.RenderGrid;
        }

        private void MenuItemLabelClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.RenderLabel = !annoCanvas.RenderLabel;
        }

        private void MenuItemIconClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.RenderIcon = !annoCanvas.RenderIcon;
        }
    }
}
