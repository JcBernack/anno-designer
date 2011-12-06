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

        public MainWindow()
        {
            InitializeComponent();
            colorPicker.StandardColors.Clear();
            colorPicker.StandardColors.Add(new ColorItem(new Color { A = 255, R = 214, G = 49, B = 49 }, "red"));
            colorPicker.StandardColors.Add(new ColorItem(new Color { A = 255, R = 171, G = 232, B = 107 }, "green"));
            colorPicker.StandardColors.Add(new ColorItem(new Color { A = 255, R = 76, G = 106, B = 222 }, "blue"));
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Button2Click(null, null);
        }

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItemGridClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.RenderGrid = !annoCanvas.RenderGrid;
        }

        private void Button1Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentObject = new AnnoObject
                {
                    Size = new Size(int.Parse(textBoxWidth.Text), int.Parse(textBoxHeight.Text)),
                    Color = colorPicker.SelectedColor
                };
                annoCanvas.SetCurrentObject(_currentObject);
            }
            catch
            {
                
            }
        }

        private void SetModeButtons(Button activeButton)
        {
            button2.IsEnabled = true;
            //button3.IsEnabled = true;
            button4.IsEnabled = true;
            activeButton.IsEnabled = false;
        }

        private void Button3Click(object sender, RoutedEventArgs e)
        {
            //SetModeButtons(button3);
            //annoCanvas.DesignMode = DesignMode.Select;
        }

        private void Button2Click(object sender, RoutedEventArgs e)
        {
            SetModeButtons(button2);
            annoCanvas.DesignMode = DesignMode.New;
        }

        private void Button4Click(object sender, RoutedEventArgs e)
        {
            SetModeButtons(button4);
            annoCanvas.DesignMode = DesignMode.Remove;
        }

        private void MenuItemNewClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.ClearPlacedObjects();
        }

        private void MenuItemSaveAsClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.SaveToFile();
        }
    }
}
