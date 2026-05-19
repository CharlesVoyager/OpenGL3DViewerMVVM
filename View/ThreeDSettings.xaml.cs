using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

#nullable disable

namespace OpenGL3DViewerMVVM.View
{
    public class BrushToFloatConverter : IValueConverter
    {
        // Background Brush to "R, G, B" float string for display in ToolTip.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var color = brush.Color;
                // Convert 0-255 byte values to 0.0-1.0 floats
                float r = color.R / 255f;
                float g = color.G / 255f;
                float b = color.B / 255f;
                return $"{r:F2}, {g:F2}, {b:F2}";
            }
            return "0.00, 0.00, 0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class UIntToBrushConverter : IValueConverter
    {
        // uint to Background Brush.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is uint argb)
            {
                byte a = (byte)((argb >> 24) & 0xFF);
                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >>  8) & 0xFF);
                byte b = (byte)( argb        & 0xFF);
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b));
            }
            return Brushes.Transparent;
        }

        // Background Brush to unit.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var c = brush.Color;
                return (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
            }
            return 0u;
        }
    }

    public class AppSettings : ViewModelBase
    {
        // Printer area diemnsions in millimeters.  These are used to draw the printer bed and frame.
        uint _printAreaWidth = 256;
        uint _printAreaDepth = 256;
        uint _printAreaHeight = 200;
        public uint PrintAreaWidth { get { return _printAreaWidth; } set { _printAreaWidth = value; OnPropertyChanged(); } }     // x-axis direction
        public uint PrintAreaDepth { get { return _printAreaDepth; } set { _printAreaDepth = value; OnPropertyChanged(); } }     // y-axis direction
        public uint PrintAreaHeight { get { return _printAreaHeight; } set { _printAreaHeight = value; OnPropertyChanged(); } }  // z-axis direction
        // <>

        // Initial OpenGL Client Size
        int _initialClientSizeWidth = 1024;
        int _initialClientSizeHeight = 768;
        public int InitialClientSizeWidth { get { return _initialClientSizeWidth; } set { _initialClientSizeWidth = value; OnPropertyChanged(); } }
        public int InitialClientSizeHeight { get { return _initialClientSizeHeight; } set { _initialClientSizeHeight = value; OnPropertyChanged(); } }
        // <>

        // Minimum OpenGL Client Size to prevent extremely small windows that can cause rendering issues
        int _minClientSizeWidth = 830;
        int _minClientSizeHeight = 700;
        public int MinClientSizeWidth { get { return _minClientSizeWidth; } set { _minClientSizeWidth = value; OnPropertyChanged(); } }
        public int MinClientSizeHeight { get { return _minClientSizeHeight; } set { _minClientSizeHeight = value; OnPropertyChanged(); } }
        // <>

        // UseVBOs and OpenGLVersion will be updated in OnLoad of ThreeDControl.
        bool _useVBOs = false;
        float _openGLVersion = 1.0f;

        public bool UseVBOs { get { return _useVBOs; } set { _useVBOs = value; OnPropertyChanged(); } }
        public float OpenGLVersion { get { return _openGLVersion; } set { _openGLVersion = value; OnPropertyChanged(); } } // Version for feature detection
        // <>

        uint _backgroundTopColor = 0xFFF5F5F5;
        uint _backgroundBottomColor = 0xFF000000;
        uint _facesColor = 0xFF4169E1;
        uint _edgesColor = 0xFFA9A9A9;
        uint _selectedFacesColor = 0xFF6495ED;
        uint _printerBaseColor = 0xFFDCDCDC;
        uint _printerFrameColor = 0xFF000000;
        uint _outsidePrintbedColor = 0xFF000000;

        public uint BackgroundTopColor { get { return _backgroundTopColor; } set { _backgroundTopColor = value; OnPropertyChanged(); } }
        public uint BackgroundBottomColor { get { return _backgroundBottomColor; } set { _backgroundBottomColor = value; OnPropertyChanged(); } }
        public uint FacesColor { get { return _facesColor; } set { _facesColor = value; OnPropertyChanged(); } }
        public uint EdgesColor { get { return _edgesColor; } set { _edgesColor = value; OnPropertyChanged(); } }
        public uint SelectedFacesColor { get { return _selectedFacesColor; } set { _selectedFacesColor = value; OnPropertyChanged(); } }
        public uint PrinterBaseColor { get { return _printerBaseColor; } set { _printerBaseColor = value; OnPropertyChanged(); } }
        public uint PrinterFrameColor { get { return _printerFrameColor; } set { _printerFrameColor = value; OnPropertyChanged(); } }
        public uint OutsidePrintbedColor { get { return _outsidePrintbedColor; } set { _outsidePrintbedColor = value; OnPropertyChanged(); } }


        bool _showEdges = false;
        bool _showFaces = true;
        bool _showPrintbed = true;

        public bool ShowEdges { get { return _showEdges; } set { _showEdges = value; OnPropertyChanged(); } }
        public bool ShowFaces { get { return _showFaces; } set { _showFaces = value; OnPropertyChanged(); } }
        public bool ShowPrintbed { get { return _showPrintbed;  } set { _showPrintbed = value; OnPropertyChanged(); } }

        uint _selectionBoxColor = 0xFFFFFFFF;
        uint _errorModelColor = 0xFFFF0000;
        uint _insideFacesColor = 0xFF000000;
        uint _modelColor = 0xFF1EB41E;  // Updated on 2026/5/11. Previous: 0xFF6BA3C6;

        public uint SelectionBoxColor { get { return _selectionBoxColor; } set { _selectionBoxColor = value; OnPropertyChanged(); } }
        public uint ErrorModelColor { get { return _errorModelColor; } set { _errorModelColor = value; OnPropertyChanged(); } }
        public uint InsideFacesColor { get { return _insideFacesColor; } set { _insideFacesColor = value; OnPropertyChanged(); } }
        public uint ModelColor { get { return _modelColor; } set { _modelColor = value; OnPropertyChanged(); } }

        //
        // Light Settings
        // 
        float _keyDirX = -0.6f;
        float _keyDirY = 1.0f;
        float _keyDirZ = 0.0f;
        uint _keyColor = 0xFFFFFAF2;
        float _keyStr = 1.8f;

        public float KeyDirX { get { return _keyDirX; } set { _keyDirX = value; OnPropertyChanged(); } }
        public float KeyDirY { get { return _keyDirY; } set { _keyDirY = value; OnPropertyChanged(); } }
        public float KeyDirZ { get { return _keyDirZ; } set { _keyDirZ = value; OnPropertyChanged(); } }
        public uint KeyColor { get { return _keyColor; } set { _keyColor = value; OnPropertyChanged(); } }
        public float KeyStr { get { return _keyStr; } set { _keyStr = value; OnPropertyChanged(); } }
        
        float _fillDirX = 0.8f;
        float _fillDirY = 0.3f;
        float _fillDirZ = 0.5f;
        uint _fillColor = 0xFFCCE0FF;
        float _fillStr = 1.2f;

        public float FillDirX { get { return _fillDirX; } set { _fillDirX = value; OnPropertyChanged(); } }
        public float FillDirY { get { return _fillDirY; } set { _fillDirY = value; OnPropertyChanged(); } }
        public float FillDirZ { get { return _fillDirZ; } set { _fillDirZ = value; OnPropertyChanged(); } }
        public uint FillColor { get { return _fillColor; } set { _fillColor = value; OnPropertyChanged(); } }
        public float FillStr { get { return _fillStr; } set { _fillStr = value; OnPropertyChanged(); } }

        float _backDirX = 0.1f;
        float _backDirY = -0.5f;
        float _backDirZ = -1.0f;
        uint _backColor = 0xFFE6EBFF;
        float _backStr = 0.9f;

        public float BackDirX { get { return _backDirX; } set { _backDirX = value; OnPropertyChanged(); } }
        public float BackDirY { get { return _backDirY; } set { _backDirY = value; OnPropertyChanged(); } }
        public float BackDirZ { get { return _backDirZ; } set { _backDirZ = value; OnPropertyChanged(); } }
        public uint BackColor { get { return _backColor; } set { _backColor = value; OnPropertyChanged(); } }
        public float BackStr { get { return _backStr; } set { _backStr = value; OnPropertyChanged(); } }

        uint _skyColor = 0xFF99B2E6;
        uint _groundColor = 0xFF40332E;
        float _ambientStr = 1.2f;

        public uint SkyColor { get { return _skyColor; } set { _skyColor = value; OnPropertyChanged(); } }
        public uint GroundColor { get { return _groundColor; } set { _groundColor = value; OnPropertyChanged(); } }
        public float AmbientStr { get { return _ambientStr; } set { _ambientStr = value; OnPropertyChanged(); } }
    }

    public class SettingsService
    {
        private static readonly SettingsService _instance = new SettingsService();
        public static SettingsService Instance => _instance;

        private readonly string _settingsPath;
        private readonly JsonSerializerOptions _jsonOptions;
        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            string assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;  // EX: OpenGL3DViewerMVVM

            // Store in %AppData%\MyApp\settings.json (Windows)
            // or ~/.config/MyApp/settings.json (Linux/macOS)
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var appFolder = Path.Combine(appDataFolder, assemblyName);

            // Create folder if it doesn't exist
            Directory.CreateDirectory(appFolder);

            _settingsPath = Path.Combine(appFolder, "Settings.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            Settings = Load();
        }

        /// <summary>
        /// Loads settings from disk. Returns defaults if file doesn't exist.
        /// </summary>
        private AppSettings Load()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    Console.WriteLine("No settings file found. Using defaults.");
                    return new AppSettings();
                }

                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
                       ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load settings: {ex.Message}. Using defaults.");
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves current settings to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(SettingsService.Instance.Settings, _jsonOptions);
                File.WriteAllText(_settingsPath, json);
                Debug.WriteLine($"Settings saved to: {_settingsPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets settings back to defaults and saves.
        /// </summary>
        public void Reset()
        {
            var settingsDefault = new AppSettings();
            var properties = settingsDefault.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(settingsDefault);
                prop.SetValue(Settings, value);
            }

            Save();
            Console.WriteLine("Settings reset to defaults.");
        }

        public string GetSettingsPath() => _settingsPath;
    }

    public partial class ThreeDSettings : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        // ── Constructor ──────────────────────────────────────────────────────────
        public ThreeDSettings()
        {
            InitializeComponent();

            DataContext = SettingsService.Instance.Settings;
            MainWindow.main.languageChanged += translate;
        }

        public void translate()
        {
            // Localisation hook — populate as needed.
        }

        // ── Color picker (replaces WinForms ColorDialog) ─────────────────────────
        private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
                PickColor(border);
        }

        void PickColor(Border border)
        {
            // Get current color from the border's background
            Color initialColor = Colors.White;
            if (border.Background is SolidColorBrush scb)
            {
                initialColor = scb.Color;
            }

            // Create WPF Color Picker Window
            var colorPickerWindow = new Window
            {
                Title = "Pick a Color",
                Width = 400,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(border),
                ResizeMode = ResizeMode.NoResize
            };

            // Selected color (will be updated on confirm)
            System.Windows.Media.Color selectedColor = initialColor;

            // --- Layout ---
            var mainStack = new StackPanel { Margin = new Thickness(15) };

            // Preview Box
            var previewBorder = new Border
            {
                Height = 40,
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(initialColor)
            };
            mainStack.Children.Add(previewBorder);

            // --- RGB + Hex Inputs ---
            byte r = initialColor.R, g = initialColor.G, b = initialColor.B, a = initialColor.A;

            // Helper: rebuild color and update preview
            Action updatePreview = null;

            // Hex Input
            var hexBox = new TextBox
            {
                Text = $"#{a:X2}{r:X2}{g:X2}{b:X2}",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(4)
            };

            // Sliders for A, R, G, B
            Slider MakeSlider(byte value) => new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = value,
                TickFrequency = 1,
                IsSnapToTickEnabled = true
            };

            TextBox MakeValueBox(byte value) => new TextBox
            {
                Text = value.ToString(),
                Width = 40,
                Padding = new Thickness(2),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var sliderA = MakeSlider(a); var boxA = MakeValueBox(a);
            var sliderR = MakeSlider(r); var boxR = MakeValueBox(r);
            var sliderG = MakeSlider(g); var boxG = MakeValueBox(g);
            var sliderB = MakeSlider(b); var boxB = MakeValueBox(b);

            // Sync slider <-> textbox <-> preview
            bool updating = false;
            updatePreview = () =>
            {
                if (updating) return;
                updating = true;
                selectedColor = System.Windows.Media.Color.FromArgb(
                    (byte)sliderA.Value, (byte)sliderR.Value,
                    (byte)sliderG.Value, (byte)sliderB.Value);
                previewBorder.Background = new SolidColorBrush(selectedColor);
                boxA.Text = ((byte)sliderA.Value).ToString();
                boxR.Text = ((byte)sliderR.Value).ToString();
                boxG.Text = ((byte)sliderG.Value).ToString();
                boxB.Text = ((byte)sliderB.Value).ToString();
                hexBox.Text = $"#{(byte)sliderA.Value:X2}{(byte)sliderR.Value:X2}" +
                              $"{(byte)sliderG.Value:X2}{(byte)sliderB.Value:X2}";
                updating = false;

                // Allow live preview of changes without needing to click OK.
                var property = typeof(AppSettings).GetProperty(border.Tag.ToString());
                if (property != null)
                {
                    uint value = (uint)(((byte)sliderA.Value << 24) + ((byte)sliderR.Value << 16) + ((byte)sliderG.Value << 8) + (byte)sliderB.Value);
                    property.SetValue(SettingsService.Instance.Settings, value);
                    MainWindow.main.threeDControl.UpdateChanges();
                }
                // <>
            };

            void BindSliderBox(Slider slider, TextBox box)
            {
                slider.ValueChanged += (_, __) => updatePreview();
                box.TextChanged += (_, __) =>
                {
                    if (updating) return;
                    if (byte.TryParse(box.Text, out byte val))
                    {
                        updating = true;
                        slider.Value = val;
                        updating = false;
                        updatePreview();
                    }
                };
            }

            BindSliderBox(sliderA, boxA);
            BindSliderBox(sliderR, boxR);
            BindSliderBox(sliderG, boxG);
            BindSliderBox(sliderB, boxB);

            hexBox.TextChanged += (_, __) =>
            {
                if (updating) return;
                var hex = hexBox.Text.TrimStart('#');
                if (hex.Length == 8 &&
                    byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte ha) &&
                    byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte hr) &&
                    byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte hg) &&
                    byte.TryParse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out byte hb))
                {
                    updating = true;
                    sliderA.Value = ha; sliderR.Value = hr;
                    sliderG.Value = hg; sliderB.Value = hb;
                    updating = false;
                    updatePreview();
                }
            };

            // Row builder helper
            Grid MakeRow(string label, Slider slider, TextBox box)
            {
                var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var lbl = new TextBlock
                {
                    Text = label,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(lbl, 0);
                Grid.SetColumn(slider, 1);
                Grid.SetColumn(box, 2);

                grid.Children.Add(lbl);
                grid.Children.Add(slider);
                grid.Children.Add(box);
                return grid;
            }

            mainStack.Children.Add(new TextBlock { Text = "Hex (AARRGGBB):", FontWeight = FontWeights.Bold });
            mainStack.Children.Add(hexBox);
            mainStack.Children.Add(MakeRow("A", sliderA, boxA));
            mainStack.Children.Add(MakeRow("R", sliderR, boxR));
            mainStack.Children.Add(MakeRow("G", sliderG, boxG));
            mainStack.Children.Add(MakeRow("B", sliderB, boxB));

            // OK / Cancel buttons
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var okBtn = new Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(4),
                IsDefault = true
            };

            okBtn.Click += (_, __) => { colorPickerWindow.Close(); };

            btnPanel.Children.Add(okBtn);
            mainStack.Children.Add(btnPanel);

            colorPickerWindow.Content = mainStack;
            colorPickerWindow.ShowDialog();
        }

        // ── Event handlers ───────────────────────────────────────────────────────
        private void CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (MainWindow.main.threeDControl != null)
                MainWindow.main.threeDControl.UpdateChanges();
        }

        /// <summary>
        /// Validates that the TextBox contains a valid float.
        /// Mirrors WinForms float_Validating / ErrorProvider pattern using a red border.
        /// </summary>
        private void float_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                bool valid = float.TryParse(tb.Text, out _);
                tb.BorderBrush = valid
                    ? SystemColors.ControlDarkBrush
                    : Brushes.Red;
                tb.ToolTip = valid ? null : "Not a number";
            }
        }

        private void uint_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                bool valid = uint.TryParse(tb.Text, out _);
                tb.BorderBrush = valid
                    ? SystemColors.ControlDarkBrush
                    : Brushes.Red;
                tb.ToolTip = valid ? null : "Not a number";
            }
        }

        private void ThreeDSettings_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true; // Prevent the window from actually closing
            this.Hide();
        }

   
        // Slider values changed.
        private void LightSetting_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.main.threeDControl != null)
                MainWindow.main.threeDControl.UpdateChanges();
        }

        // TextBox values changed.
        private void LightSetting_ValueChanged(object sender, TextChangedEventArgs e)
        {
            if (MainWindow.main.threeDControl != null)
                MainWindow.main.threeDControl.UpdateChanges();
        }

        /*  Light default settings
            // --- Three-point studio rig ---
            const vec3  keyDir   = normalize(vec3(-0.6, 1.0, 0.8));
            const vec3  keyColor = vec3(1.00, 0.98, 0.95);                  //#FFFAF2
            const float keyStr   = 1.8;

            const vec3  fillDir   = normalize(vec3(0.8, 0.3, 0.5));
            const vec3  fillColor = vec3(0.80, 0.88, 1.00);                 //#CCE0FF
            const float fillStr   = 1.2;

            const vec3  backDir   = normalize(vec3(0.1, -0.5, -1.0));
            const vec3  backColor = vec3(0.90, 0.92, 1.00);                 //#E6EBFF
            const float backStr   = 0.9;

            // --- Hemisphere ambient ---
            const vec3  skyColor    = vec3(0.60, 0.70, 0.90);               //#99B2E6
            const vec3  groundColor = vec3(0.25, 0.20, 0.18);               //#40332E
            const float ambientStr  = 1.2;
         */

        private void ResetLightSettingsToDefault_Click(object sender, RoutedEventArgs e)
        {
            AppSettings defaultSettings = new AppSettings();

            SettingsService.Instance.Settings.KeyDirX = defaultSettings.KeyDirX;
            SettingsService.Instance.Settings.KeyDirY = defaultSettings.KeyDirY;
            SettingsService.Instance.Settings.KeyDirZ = defaultSettings.KeyDirZ;
            SettingsService.Instance.Settings.KeyColor = defaultSettings.KeyColor;
            SettingsService.Instance.Settings.KeyStr = defaultSettings.KeyStr;

            SettingsService.Instance.Settings.FillDirX = defaultSettings.FillDirX;
            SettingsService.Instance.Settings.FillDirY = defaultSettings.FillDirY;
            SettingsService.Instance.Settings.FillDirZ = defaultSettings.FillDirZ;
            SettingsService.Instance.Settings.FillColor = defaultSettings.FillColor;
            SettingsService.Instance.Settings.FillStr = defaultSettings.FillStr;
         
            SettingsService.Instance.Settings.BackDirX = defaultSettings.BackDirX;
            SettingsService.Instance.Settings.BackDirY = defaultSettings.BackDirY;
            SettingsService.Instance.Settings.BackDirZ = defaultSettings.BackDirZ                   ;
            SettingsService.Instance.Settings.BackColor = defaultSettings.BackColor;
            SettingsService.Instance.Settings.BackStr = defaultSettings.BackStr;

            SettingsService.Instance.Settings.SkyColor = defaultSettings.SkyColor;
            SettingsService.Instance.Settings.GroundColor = defaultSettings.GroundColor;
            SettingsService.Instance.Settings.AmbientStr = defaultSettings.AmbientStr;
        }

        private void ThreeDSettings_Closed(object sender, EventArgs e)
        {
            SettingsService.Instance.Save();
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.Instance.Reset();
            MainWindow.main.threeDControl.UpdateChanges();
        }

        private void OpenSettingsFileFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(SettingsService.Instance.GetSettingsPath()),
                UseShellExecute = true
            });
        }
    }
}
