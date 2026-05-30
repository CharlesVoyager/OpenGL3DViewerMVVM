using OpenGL3DViewerMVVM.MeshIOLib;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using OpenGL3DViewerMVVM.View;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

#nullable disable

namespace OpenGL3DViewerMVVM
{
    /// <summary>
    /// A RadioButton that can be unchecked by clicking it again,
    /// while still preventing two buttons in the same group from
    /// being checked at the same time.
    /// </summary>
    public class ToggleRadioButton : RadioButton
    {
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // If already checked, uncheck and swallow the event so the
            // base class does not immediately re-check it.
            if (IsChecked == true)
            {
                IsChecked = false;
                e.Handled = true;   // prevent base from re-checking
                return;
            }

            base.OnMouseLeftButtonDown(e);
        }
    }

    public partial class MainWindow : Window
    {
        public static MainWindow main = null;
        public static readonly ManualResetEventSlim _mainWindowReady = new ManualResetEventSlim(false);

        public ThreeDControl threeDControl = null;
        public ViewModel viewModel = null;
        public ThreeDSettings threeDSettings = null;
        public STLComposer stlComposer = null;
        public ThreeDCamera threeDCamera = null;

        double dpiX, dpiY;

        public MainWindow(ThreeDControl threeDCtrl)
        {
            main = this;

            // Retrieve DPI from WPF presentation source after initialization
            Loaded += (s, e) =>
            {
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                    dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                }
            };

            // ThreeDControl is created in the main thread (not WPF thread) in App.Main() and passed to MainWindow via constructor.
            threeDControl = threeDCtrl;

            // ViewModel for MVVM pattern used on STLComposer and other UI user controls.
            viewModel = new ViewModel();

            // ThreeDSettings
            threeDSettings = new ThreeDSettings();
            threeDSettings.Hide();

            // STLComposer
            stlComposer = new STLComposer();
            stlComposer.Hide();

            // Camera
            threeDCamera = new ThreeDCamera();

            InitializeComponent();
          
            DataContext = viewModel;
       
            initializeUi();
            _mainWindowReady.Set();
        }

        // NOTE: MainWindow is not fully overlay on the ThreeDControl.
        // If the user drops a file on the ThreeDControl (GameWindow), the drop event in ThreeDControl will be triggered.
        private async void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var modelIO = new MeshIOWrapper();
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (modelIO.IsFileSupported(file))
                        await viewModel.ExecuteAddAsync(file);
                }
            }
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Delete)
                {
                    viewModel.DeleteModel();
                }
                else if (e.Key == Key.Subtract) 
                {
                    threeDControl.ZoomOutKeyHandling(null, null);
                }
                else if (e.Key == Key.Add)
                {
                    threeDControl.ZoomInKeyHandling(null, null);
                }
            }
            catch { }
        }

        public void UpdateLocation(double x, double y)
        {
            Left = x / dpiX * 96;
            Top = y / dpiY * 96;
        }

        public void UpdateSize(double width, double height)
        {
            Width = width / dpiX * 96;
            Height = height / dpiY * 96 + 28;
        }

        //── UI (WPF) ────────────────────────────────────────────────
        DispatcherTimer timer;
        private ContextMenu _contextMenu;
        private void initializeUi()
        {
            VisualStateManager.GoToState(UI_view, "StateHidden", true);
            VisualStateManager.GoToState(UI_move, "StateHidden", true);
            VisualStateManager.GoToState(UI_rotate, "StateHidden", true);
            VisualStateManager.GoToState(UI_resize_advance, "StateHidden", true);
            VisualStateManager.GoToState(UI_object_information, "StateHidden", true);

            Trans.trans?.languageChanged += translate;

            // Retrieve the context menu from resources
            _contextMenu = (System.Windows.Controls.ContextMenu)this.Resources["ViewerContextMenu"];

            // Wire up click handlers
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[0]).Click += (s, e) => viewModel.LandModel();
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[1]).Click += (s, e) => viewModel.ResetModel();
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[2]).Click += (s, e) => viewModel.DeleteModel();
            // index 3 is Separator
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[4]).Click += (s, e) => viewModel.DoMmToInch();
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[5]).Click += (s, e) => viewModel.DoInchToMm();
            // index 6 is Separator
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[7]).Click += (s, e) => viewModel.CloneModel();
            // index 8 is Separator
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[9]).Click += (s, e) => stlComposer.Show();
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[10]).Click += (s, e) => threeDSettings.Show();

            // About
            gridAbout.Visibility = Visibility.Hidden;

            // Memory Monitor
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timerTickMemoryMonitor;
            ShowMemoryMonitor(SettingsService.Instance.Settings.ShowMemoryMonitor);

            // Enable Viewer Mode 
            EnableViewerMode(SettingsService.Instance.Settings.EnableViewerMode);
        }

        /// <summary>
        /// Called from ThreeDControl (GL thread) via Dispatcher.InvokeAsync.
        /// hasModel controls which items are visible.
        /// </summary>
        public void ShowContextMenu(bool isModelSelected)
        {
            if (SettingsService.Instance.Settings.EnableViewerMode)
            {
                ((MenuItem)_contextMenu.Items[0]).IsEnabled = false;    // Land Model
                ((MenuItem)_contextMenu.Items[1]).IsEnabled = false;    // Reset Model
                ((MenuItem)_contextMenu.Items[2]).IsEnabled = false;    // Delete Model
                // Separator
                ((MenuItem)_contextMenu.Items[4]).IsEnabled = false;    // mm to inch
                ((MenuItem)_contextMenu.Items[5]).IsEnabled = false;    // inch to mm
                // Separator
                ((MenuItem)_contextMenu.Items[7]).IsEnabled = false;    // Clone Model
            }
            else
            {
                ((MenuItem)_contextMenu.Items[0]).IsEnabled = isModelSelected;  // Land Model      
                ((MenuItem)_contextMenu.Items[1]).IsEnabled = isModelSelected;  // Reset Model
                ((MenuItem)_contextMenu.Items[2]).IsEnabled = isModelSelected;  // Delete Model
                // Separator
                ((MenuItem)_contextMenu.Items[4]).IsEnabled = isModelSelected;  // mm to inch
                ((MenuItem)_contextMenu.Items[5]).IsEnabled = isModelSelected;  // inch to mm
                // Separator
                ((MenuItem)_contextMenu.Items[7]).IsEnabled = isModelSelected;  // Clone Model
            }
            _contextMenu.IsOpen = true;
        }

        private void translate()
        {
            view_toggleButton.ToolTip = Trans.T("B_VIEW");
            move_toggleButton.ToolTip = Trans.T("B_MOVE");
            rotate_toggleButton.ToolTip = Trans.T("B_ROTATE");
            resize_toggleButton.ToolTip = Trans.T("B_SCALE");
            info_toggleButton.ToolTip = Trans.T("B_INFO");
            remove_toggleButton.ToolTip = Trans.T("B_REMOVE");
            btnImport.ToolTip = Trans.T("B_IMPORT");
            about_toggleButton.ToolTip = Trans.T("B_ABOUT");

            view_toggleButton.Content = Trans.T("B_VIEW");
            move_toggleButton.Content = Trans.T("B_MOVE");
            rotate_toggleButton.Content = Trans.T("B_ROTATE");
            resize_toggleButton.Content = Trans.T("B_SCALE");
            info_toggleButton.Content = Trans.T("B_INFO");
            remove_toggleButton.Content = Trans.T("B_REMOVE");
            btnImport.Content = Trans.T("B_IMPORT");
            about_toggleButton.Content = Trans.T("B_ABOUT");
        }

        // Move/Rotate/Scale/Info/Remove button visibility changed.
        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            if (isVisible)
            {
                view_toggleButton.IsChecked = false;
                move_toggleButton.IsChecked = false;
                rotate_toggleButton.IsChecked = false;
                resize_toggleButton.IsChecked = false;
                info_toggleButton.IsChecked = false;
                remove_toggleButton.IsChecked = false;
                about_toggleButton.IsChecked = false;
            }
            else
            {            
                VisualStateManager.GoToState(UI_view, "StateHidden", true);
                VisualStateManager.GoToState(UI_move, "StateHidden", true);
                VisualStateManager.GoToState(UI_rotate, "StateHidden", true);
                VisualStateManager.GoToState(UI_resize_advance, "StateHidden", true);
                VisualStateManager.GoToState(UI_object_information, "StateHidden", true);
                about_toggleButton.IsChecked = false;   // Hide grdAbout if about_toggleButton is checked.
            }
        }

        private void view_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_view, "StateVisible", true);
        }

        private void view_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_view, "StateHidden", true);
        }

        public void move_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_move, "StateVisible", true);

            // The model is required to reset the slider minimum and maximum after scale or rotate.
            UI_move.SetSliderMinimumMaximum();
        }

        public void move_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_move, "StateHidden", true);
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            btnImport.IsEnabled = false;
            await viewModel.ExecuteAddAsync();

            // If viewer mode is enabled, only allow one model to be loaded, so keep the Import button disabled.
            if (SettingsService.Instance.Settings.EnableViewerMode != true 
                || viewModel.Models.Count == 0) // Cancel to add model.
                btnImport.IsEnabled = true;

            btnImport.IsChecked = false;
        }

        private void AboutToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            gridAbout.Visibility = Visibility.Visible;



            DebugLog();
        }

        private void button_closeAbout_Click(object sender, RoutedEventArgs e)
        {
            about_toggleButton.IsChecked = false;
        }

        private void AboutToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            gridAbout.Visibility = Visibility.Hidden;
        }

        private void rotate_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_rotate, "StateVisible", true);
        }

        private void rotate_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_rotate, "StateHidden", true);
        }

        // Scale
        public void resize_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_resize_advance, "StateVisible", true);
        }

        private void resize_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_resize_advance, "StateHidden", true);
        }

        private void info_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_object_information, "StateVisible", true);
        }

        private void info_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_object_information, "StateHidden", true);
        }

        public void remove_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            UI_move.slider_moveX.Minimum = -1000;
            UI_move.slider_moveX.Maximum = 1000;
            UI_move.slider_moveY.Minimum = -1000;
            UI_move.slider_moveY.Maximum = 1000;

            viewModel.DeleteModel();
        }

        private void zoomin_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            threeDControl.ZoomInKeyHandling(null, null);
        }

        private void zoomout_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            threeDControl.ZoomOutKeyHandling(null, null);
        }

        void DebugLog()
        {
        }

        private void timerTickMemoryMonitor(object sender, EventArgs e)
        {
            memoryUsageLabel.Content = RamTools.getCurMemoryUsed().ToString() + " MB";
        }

        public void ShowMemoryMonitor(bool show)
        {
            if (show)
            {
                grdMemoryMonitor.Visibility = Visibility.Visible;
                timer.Start();
            }
            else
            {
                grdMemoryMonitor.Visibility = Visibility.Hidden;
                timer.Stop();
            }
        }

        public void EnableViewerMode(bool enable)
        {
            if (enable)
            {
                // Hide all button on left panel.
                view_toggleButton.Visibility = Visibility.Collapsed;
                move_toggleButton.Visibility = Visibility.Collapsed;
                rotate_toggleButton.Visibility = Visibility.Collapsed;
                resize_toggleButton.Visibility = Visibility.Collapsed;
                info_toggleButton.Visibility = Visibility.Collapsed;
                remove_toggleButton.Visibility = Visibility.Collapsed;

                VisualStateManager.GoToState(UI_move, "StateHidden", true);
                VisualStateManager.GoToState(UI_rotate, "StateHidden", true);
                VisualStateManager.GoToState(UI_resize_advance, "StateHidden", true);
                VisualStateManager.GoToState(UI_object_information, "StateHidden", true);

                // Fit Model
                viewModel.FitModel();

                // If GLB file is loaded, rotate the model 90 degree on X axis to make it upright, because GLB file is usually created in Y-up coordinate system.
                if (viewModel.SelectedModel?.Name.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) == true)
                    viewModel.SelectedModel?.RotationX = 90;

                // Not allow to load multi-model.
                if (viewModel.Models.Count > 0)
                    btnImport.IsEnabled = false;
            }
            else
            {
                view_toggleButton.Visibility = Visibility.Visible;
                move_toggleButton.Visibility = Visibility.Visible;
                rotate_toggleButton.Visibility = Visibility.Visible;
                resize_toggleButton.Visibility = Visibility.Visible;
                info_toggleButton.Visibility = Visibility.Visible;
                remove_toggleButton.Visibility = Visibility.Visible;

                btnImport.IsEnabled = true;
            }
            MainWindow.main.threeDControl.UpdateChanges();  // Update show or hide of Bounding Box.
        }
    }
}
