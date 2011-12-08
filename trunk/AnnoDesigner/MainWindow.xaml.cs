using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        private List<string> _icons;

        public MainWindow()
        {
            InitializeComponent();
            // add color presets
            colorPicker.StandardColors.Clear();
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(214, 49, 49), "red"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(171, 232, 107), "green"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(76, 106, 222), "blue"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(247, 150, 70), "orange"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(0, 0, 0), "black"));
            // add icons
            _icons = new List<string>(Directory.GetFiles(@"icons\", "*.png"));
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // add icons to the combobox
            comboBoxIcon.Items.Clear();
            comboBoxIcon.Items.Add(new ComboBoxItem { Content = "None" });
            _icons.ForEach(_ => comboBoxIcon.Items.Add(new ComboBoxItem { Content = Path.GetFileNameWithoutExtension(_) }));
            comboBoxIcon.SelectedIndex = 0;
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
                    Icon = comboBoxIcon.SelectedIndex == 0 ? null : _icons[comboBoxIcon.SelectedIndex],
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
