using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Windows.Controls;
using MessageBox = Microsoft.Windows.Controls.MessageBox;

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
            // add event handlers
            annoCanvas.OnCurrentObjectChange += AnnoCanvasOnCurrentObjectChange;
            annoCanvas.OnShowStatusMessage += AnnoCanvasOnShowStatusMessage;
            // add color presets
            colorPicker.StandardColors.Clear();
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(214, 49, 49), "Scheme 1 - depot"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(171, 232, 107), "Scheme 1"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(76, 106, 222), "Scheme 1"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(247, 150, 70), "Scheme 1"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(128, 128, 128), "Scheme 1 - path"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 67, 61), "Scheme 2 - depot"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(247, 0, 239), "Scheme 2- building A"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 108, 200), "Scheme 2 - field A"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 166, 0), "Scheme 2 - building B"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 209, 123), "Scheme 2 - field B"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(0, 247, 241), "Scheme 2 - factory"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(36, 255, 0), "Scheme 2 - path"));
            
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
            annoCanvas.Focus();
        }

        private void AnnoCanvasOnCurrentObjectChange(AnnoObject obj)
        {
            textBoxWidth.Text = obj.Size.Width.ToString();
            textBoxHeight.Text = obj.Size.Height.ToString();
            colorPicker.SelectedColor = obj.Color;
            textBoxLabel.Text = obj.Label;
            comboBoxIcon.SelectedIndex = _icons.FindIndex(_ => _ == obj.Icon) + 1;
            textBoxRadius.Text = obj.Radius.ToString();
        }

        private void AnnoCanvasOnShowStatusMessage(string message)
        {
            StatusBarItemStatus.Content = message;
            System.Diagnostics.Debug.WriteLine(message);
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
                    Icon = comboBoxIcon.SelectedIndex == 0 ? null : _icons[comboBoxIcon.SelectedIndex-1],
                    Radius = string.IsNullOrEmpty(textBoxRadius.Text) ? 0 : double.Parse(textBoxRadius.Text)
                };
                // do some sanity checks
                if (_currentObject.Size.Width > 0 && _currentObject.Size.Height > 0 && _currentObject.Radius >= 0)
                {
                    annoCanvas.SetCurrentObject(_currentObject);
                }
            }
            catch
            {
                MessageBox.Show("Error: Please check configuration.");
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
