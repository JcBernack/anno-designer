using System;
using System.IO;
using System.Linq;
using System.Net;
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
        private Presets _presets;
        private AnnoObject _currentObject;
        private readonly WebClient _webClient;
        private IconComboBoxItem _noIconItem;

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();
            // initialize web client
            _webClient = new WebClient();
            _webClient.DownloadStringCompleted += WebClientDownloadStringCompleted;
            // add event handlers
            annoCanvas.OnCurrentObjectChanged += UpdateUIFromObject;
            annoCanvas.OnStatusMessageChanged += StatusMessageChanged;
            annoCanvas.OnLoadedFileChanged += LoadedFileChanged;
            // add color presets
            colorPicker.StandardColors.Clear();
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(214, 49, 49), "Scheme 1 - Depot"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(76, 106, 222), "Scheme 1 - Factory"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(247, 150, 70), "Scheme 1 - Farm"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(171, 232, 107), "Scheme 1 - Field"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(128, 128, 128), "Scheme 1 - Path"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 67, 61), "Scheme 2 - Depot"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(0, 247, 241), "Scheme 2 - Factory"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(247, 0, 239), "Scheme 2- Building A"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 108, 200), "Scheme 2 - Field A"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 166, 0), "Scheme 2 - Building B"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(255, 209, 123), "Scheme 2 - Field B"));
            colorPicker.StandardColors.Add(new ColorItem(Color.FromRgb(36, 255, 0), "Scheme 2 - Path"));
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // add icons to the combobox
            comboBoxIcon.Items.Clear();
            _noIconItem = new IconComboBoxItem("None");
            comboBoxIcon.Items.Add(_noIconItem);
            foreach (var icon in annoCanvas.Icons)
            {
                comboBoxIcon.Items.Add(new IconComboBoxItem(icon.Key));
            }
            comboBoxIcon.SelectedIndex = 0;
            // check for updates on startup
            MenuItemVersion.Header = "Current version: " + Constants.Version;
            CheckForUpdates(false);
            // load presets
            treeViewPresets.Items.Clear();
            treeViewPresets.Items.Add(new BuildingTreeViewItem(new BuildingInfo { Width = 1, Height = 1, Eng = "" }) { Header = "road tile" });
            try
            {
                _presets = DataIO.LoadFromFile<Presets>(Path.Combine(App.ApplicationPath, "presets.json"));
                _presets.AddToTree(treeViewPresets);
                GroupBoxPresets.Header = string.Format("Building presets - loaded v{0}", _presets.Version);
            }
            catch (Exception ex)
            {
                GroupBoxPresets.Header = "Building presets - load failed";
                MessageBox.Show(ex.Message, "Loading the building presets failed.");
            }
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
            if (int.Parse(e.Result) > Constants.Version)
            {
                // new version found
                if (MessageBox.Show("A newer version was found, do you want to visit the project page?\nhttp://anno-designer.googlecode.com/", "Update available", MessageBoxButton.YesNo, MessageBoxImage.Asterisk, MessageBoxResult.OK) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("http://code.google.com/p/anno-designer/downloads/list");
                }
            }
            else
            {
                StatusMessageChanged("Version is up to date.");
                if ((bool)e.UserState)
                {
                    MessageBox.Show("This version is up to date.", "No updates found");
                }
            }
        }

        #endregion

        #region AnnoCanvas events

        private void UpdateUIFromObject(AnnoObject obj)
        {
            if (obj == null)
            {
                return;
            }
            // size
            textBoxWidth.Text = obj.Size.Width.ToString();
            textBoxHeight.Text = obj.Size.Height.ToString();
            // color
            colorPicker.SelectedColor = obj.Color;
            // label
            //checkBoxLabel.IsChecked = !string.IsNullOrEmpty(obj.Label);
            textBoxLabel.Text = obj.Label;
            // icon
            //checkBoxIcon.IsChecked = !string.IsNullOrEmpty(obj.Icon);
            try
            {
                comboBoxIcon.SelectedItem = string.IsNullOrEmpty(obj.Icon) ? _noIconItem : comboBoxIcon.Items.Cast<IconComboBoxItem>().Single(_ => _.IconName == Path.GetFileNameWithoutExtension(obj.Icon));
            }
            catch (Exception)
            {
                comboBoxIcon.SelectedItem = _noIconItem;
            }
            // radius
            //checkBoxRadius.IsChecked = obj.Radius > 0;
            textBoxRadius.Text = obj.Radius.ToString();
        }

        private void StatusMessageChanged(string message)
        {
            StatusBarItemStatus.Content = message;
            System.Diagnostics.Debug.WriteLine(string.Format("Status message changed: {0}", message));
        }

        private void LoadedFileChanged(string filename)
        {
            Title = string.IsNullOrEmpty(filename) ? "Anno Designer" : string.Format("{0} - Anno Designer", Path.GetFileName(filename));
            System.Diagnostics.Debug.WriteLine(string.Format("Loaded file changed: {0}", string.IsNullOrEmpty(filename) ? "(none)" : filename));
        }

        #endregion

        #region Main methods

        private static bool IsChecked(CheckBox checkBox)
        {
            return checkBox.IsChecked ?? false;
        }

        private void ApplyCurrentObject()
        {
            // parse user inputs and create new object
            _currentObject = new AnnoObject
            {
                Size = new Size(int.Parse(textBoxWidth.Text), int.Parse(textBoxHeight.Text)),
                Color = colorPicker.SelectedColor,
                Label = IsChecked(checkBoxLabel) ? textBoxLabel.Text : "",
                Icon = !IsChecked(checkBoxIcon) || comboBoxIcon.SelectedItem == _noIconItem ? null : ((IconComboBoxItem)comboBoxIcon.SelectedItem).IconName,
                Radius = !IsChecked(checkBoxRadius) || string.IsNullOrEmpty(textBoxRadius.Text) ? 0 : double.Parse(textBoxRadius.Text)
            };
            // do some sanity checks
            if (_currentObject.Size.Width > 0 && _currentObject.Size.Height > 0 && _currentObject.Radius >= 0)
            {
                annoCanvas.SetCurrentObject(_currentObject);
            }
            else
            {
                throw new Exception("Invalid building configuration.");
            }
        }

        private void ApplyPreset()
        {
            try
            {
                var item = (BuildingTreeViewItem) treeViewPresets.SelectedItem;
                if (item != null && item.BuildingInfo != null)
                {
                    var obj = item.BuildingInfo.ToAnnoObject();
                    obj.Color = colorPicker.SelectedColor;
                    UpdateUIFromObject(obj);
                    ApplyCurrentObject();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong while applying the preset.");
            }
        }

        #endregion

        #region UI events

        private void MenuItemCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonPlaceBuildingClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyCurrentObject();
            }
            catch (Exception)
            {
                MessageBox.Show("Error: Invalid building configuration.");
            }
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
        
        private void TreeViewPresetsMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ApplyPreset();
        }

        private void TreeViewPresetsKeyDown(object sender, KeyEventArgs e)
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
        //    // note: not tested!
        //    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\anno_designer");
        //    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.ad");
        //}

        #endregion
    }
}