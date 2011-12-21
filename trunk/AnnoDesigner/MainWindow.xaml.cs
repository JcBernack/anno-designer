using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
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
        private List<BuildingInfo> _presets;
        private AnnoObject _currentObject;
        private readonly List<string> _icons;
        private const int CurrentVersion = 5;
        private readonly WebClient _webClient;

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();
            // initialize web client
            _webClient = new WebClient();
            _webClient.DownloadStringCompleted += WebClientDownloadStringCompleted;
            // add event handlers
            annoCanvas.OnCurrentObjectChange += UpdateUIFromObject;
            annoCanvas.OnShowStatusMessage += ShowStatusMessage;
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
            _icons = Directory.GetFiles(Path.Combine(App.ApplicationPath, "icons"), "*.png").ToList();
            _icons.Sort();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // add icons to the combobox
            comboBoxIcon.Items.Clear();
            comboBoxIcon.Items.Add(new ComboBoxItem { Content = "None" });
            _icons.ForEach(_ => comboBoxIcon.Items.Add(new ComboBoxItem { Content = Path.GetFileNameWithoutExtension(_) }));
            comboBoxIcon.SelectedIndex = 0;
            // check for updates on startup
            MenuItemVersion.Header = "Current version: " + CurrentVersion;
            CheckForUpdates(false);
            // load presets
            _presets = DataIO.LoadFromFile<List<BuildingInfo>>(Path.Combine(App.ApplicationPath, "presets.json"));
            listViewPresets.Items.Clear();
            var excludedTemplates = new[] { "Ark", "ThirdPartyWarehouse", "ThirdpartyMilitaryBuilding" };
            _presets.Where(_ => !excludedTemplates.Contains(_.Template)).OrderBy(_ => _.GetDisplayValue()).ToList().ForEach(_ => listViewPresets.Items.Add(_.GetDisplayValue()));
            // load file given by argument
            if (!string.IsNullOrEmpty(App.FilenameArgument))
            {
                annoCanvas.OpenFile(App.FilenameArgument);
            }
        }

        #endregion

        #region Version check

        private void CheckForUpdates(bool forcedCheck)
        {
            _webClient.DownloadStringAsync(new Uri("http://anno-designer.googlecode.com/svn/trunk/version.txt"), forcedCheck);
        }

        private void WebClientDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Version check failed");
                return;
            }
            if (int.Parse(e.Result) > CurrentVersion)
            {
                // new version found
                if (MessageBox.Show("A newer version was found, do you want to visit the project page?\nhttp://anno-designer.googlecode.com/", "Update available", MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.OK) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("http://code.google.com/p/anno-designer/downloads/list");
                }
            }
            else
            {
                ShowStatusMessage("Version is up to date.");
                if ((bool)e.UserState)
                {
                    MessageBox.Show("This version is up to date.", "No updates found");
                }
            }
        }

        #endregion

        #region Main methods

        private void UpdateUIFromObject(AnnoObject obj)
        {
            textBoxWidth.Text = obj.Size.Width.ToString();
            textBoxHeight.Text = obj.Size.Height.ToString();
            colorPicker.SelectedColor = obj.Color;
            textBoxLabel.Text = obj.Label;
            comboBoxIcon.SelectedIndex = _icons.FindIndex(_ => !string.IsNullOrEmpty(obj.Icon) && _.EndsWith(obj.Icon)) + 1;
            textBoxRadius.Text = obj.Radius.ToString();
        }

        private static bool IsChecked(CheckBox checkBox)
        {
            return checkBox.IsChecked ?? false;
        }

        private void ApplyCurrentObject()
        {
            try
            {
                // parse user inputs and create new object
                _currentObject = new AnnoObject
                {
                    Size = new Size(int.Parse(textBoxWidth.Text), int.Parse(textBoxHeight.Text)),
                    Color = colorPicker.SelectedColor,
                    Label = IsChecked(checkBoxLabel) ? textBoxLabel.Text : "",
                    Icon = !IsChecked(checkBoxIcon) || comboBoxIcon.SelectedIndex == 0 ? null : _icons[comboBoxIcon.SelectedIndex - 1],
                    Radius = !IsChecked(checkBoxRadius) || string.IsNullOrEmpty(textBoxRadius.Text) ? 0 : double.Parse(textBoxRadius.Text)
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

        private void ApplyPreset()
        {
            try
            {
                var obj = _presets.Find(_ => _.GetDisplayValue() == (string)listViewPresets.SelectedItem).ToAnnoObject();
                obj.Color = colorPicker.SelectedColor;
                UpdateUIFromObject(obj);
                ApplyCurrentObject();
            }
            catch (Exception)
            {
            }
        }

        private void ShowStatusMessage(string message)
        {
            StatusBarItemStatus.Content = message;
            System.Diagnostics.Debug.WriteLine(message);
        }

        #endregion

        #region UI events

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonPlaceBuildingClick(object sender, RoutedEventArgs e)
        {
            ApplyCurrentObject();
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
            annoCanvas.ExportImage(MenuItemExportZoom.IsChecked, MenuItemExportSelection.IsChecked);
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

        private void MenuItemVersionCheckImageClick(object sender, RoutedEventArgs e)
        {
            CheckForUpdates(true);
        }
        
        private void ListViewPresetsMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ApplyPreset();
        }

        private void ListViewPresetsKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ApplyPreset();
            }
        }

        private void MenuItemResetZoomClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.ResetZoom();
        }

        private void MenuItemNormalizeClick(object sender, RoutedEventArgs e)
        {
            annoCanvas.Normalize(1);
        }

        private void MenuItemRegisterExtensionClick(object sender, RoutedEventArgs e)
        {
            // registers the anno_designer class type and adds the correct command string to pass a file argument to the application
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\anno_designer\shell\open\command", null, string.Format("\"{0}\" \"%1\"", App.ExecutablePath));
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\anno_designer\DefaultIcon", null, string.Format("\"{0}\",0", App.ExecutablePath));
            // registers the .ad file extension to the anno_designer class
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\.ad", null, "anno_designer");
        }

        //private void MenuItemRemoveExtensionClick(object sender, RoutedEventArgs e)
        //{
        //    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\anno_designer");
        //    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.ad");
        //}

        #endregion
    }
}