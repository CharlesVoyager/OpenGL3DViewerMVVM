using OpenGL3DViewerMVVM.MeshIOLib;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using OpenGL3DViewerMVVM.View;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using View3D.view;

#nullable disable

namespace View3D
{
    public delegate void languageChangedEvent();

    public partial class MainWindow : Window
    {
        public event languageChangedEvent languageChanged = null;

        public static MainWindow main = null;

        public ViewModel viewModel = null;
        public ThreeDSettings threeDSettings = null;
        public ThreeDControl threeDControl = null;
        public STLComposer stlComposer = null;
        public ThreeDCamera threeDCamera = null;

        public Trans trans = null;

        public double dpiX, dpiY;

        public static readonly ManualResetEventSlim _mainWindowReady = new ManualResetEventSlim(false);

        public MainWindow()
        {
            main = this;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);
            // Translator

            trans = new Trans(AppDomain.CurrentDomain.BaseDirectory + "Resources");

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

            if (languageChanged != null)
                languageChanged();

            _mainWindowReady.Set();
        }

        // NOTE: MainWindow is not fully overlay on the ThreeDControl.
        // If the user drops a file on the ThreeDControl (GameWindow), the drop event in ThreeDControl will be triggered.
        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var modelIO = new MeshIOWrapper();
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (modelIO.IsFileSupported(file))
                        viewModel.AddModel(file);
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
                    threeDControl.UpdateChanges();
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
        private ContextMenu _contextMenu;
        private void initializeUi()
        {
            VisualStateManager.GoToState(UI_view, "StateHidden", true);
            VisualStateManager.GoToState(UI_move, "StateHidden", true);
            VisualStateManager.GoToState(UI_rotate, "StateHidden", true);
            VisualStateManager.GoToState(UI_resize_advance, "StateHidden", true);
            VisualStateManager.GoToState(UI_object_information, "StateHidden", true);

            UI_resize_advance.btn_Scale.FontSize = 12;
            UI_resize_advance.button_mmtoinch.FontSize = 12;
            UI_resize_advance.button_inchtomm.FontSize = 12;
            UI_resize_advance.lbl_Size.FontSize = 12;

            move_toggleButton.FontSize = 12;
            import_button.FontSize = 12;

            languageChanged += translate;

            // Retrieve the context menu from resources
            _contextMenu = (System.Windows.Controls.ContextMenu)this.Resources["ViewerContextMenu"];

            // Wire up click handlers
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[0]).Click += (s, e) => viewModel.SelectedModel?.Land();
            ((System.Windows.Controls.MenuItem)_contextMenu.Items[1]).Click += (s, e) => viewModel.SelectedModel?.Reset();
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
        }

        /// <summary>
        /// Called from ThreeDControl (GL thread) via Dispatcher.InvokeAsync.
        /// hasModel controls which items are visible.
        /// </summary>
        public void ShowContextMenu(bool isModelSelected)
        {
            if (isModelSelected == false) return;

            // Must be called on the WPF thread
            _contextMenu.Items.Cast<FrameworkElement>()
                .Where(item => item is System.Windows.Controls.MenuItem)
                .ToList()
                .ForEach(item => item.Visibility =
                    isModelSelected ? Visibility.Visible : Visibility.Collapsed);

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
            import_button.ToolTip = Trans.T("B_IMPORT");
            about_button.ToolTip = Trans.T("B_ABOUT");

            view_toggleButton.Content = Trans.T("B_VIEW");
            move_toggleButton.Content = Trans.T("B_MOVE");
            rotate_toggleButton.Content = Trans.T("B_ROTATE");
            resize_toggleButton.Content = Trans.T("B_SCALE");
            info_toggleButton.Content = Trans.T("B_INFO");
            remove_toggleButton.Content = Trans.T("B_REMOVE");
            import_button.Content = Trans.T("B_IMPORT");
            about_button.Content = Trans.T("B_ABOUT");
        }

        // Move/Rotate/Scale/Info/Remove button visibility changed.
        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            view_toggleButton.IsChecked = false;
            VisualStateManager.GoToState(UI_view, "StateHidden", true);

            bool isVisible = (bool)e.NewValue;
            if (isVisible)
            {
                move_toggleButton.IsChecked = false;
                rotate_toggleButton.IsChecked = false;
                resize_toggleButton.IsChecked = false;
                info_toggleButton.IsChecked = false;
                remove_toggleButton.IsChecked = false;
            }
            else
            {
                VisualStateManager.GoToState(UI_move, "StateHidden", true);
                VisualStateManager.GoToState(UI_rotate, "StateHidden", true);
                VisualStateManager.GoToState(UI_resize_advance, "StateHidden", true);
                VisualStateManager.GoToState(UI_object_information, "StateHidden", true);
            }
        }

        private void view_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_view, "StateVisible", true);

            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;
        }

        private void view_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_view, "StateHidden", true);
            Focus();
        }

        public void move_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_move, "StateVisible", true);
            view_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;
        }

        public void move_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_move, "StateHidden", true);
            Focus();
        }

        private void import_button_Click(object sender, RoutedEventArgs e)
        {
            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            viewModel.AddModel();
        }

        private void about_button_Click(object sender, RoutedEventArgs e)
        {
            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;

            gridAbout.Visibility = Visibility.Visible;

            DebugLog();
        }

        private void rotate_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_rotate, "StateVisible", true);
            
            UI_rotate.textRotX.Text = stlComposer.textRotX.Text;
            UI_rotate.textRotY.Text = stlComposer.textRotY.Text;
            UI_rotate.textRotZ.Text = stlComposer.textRotZ.Text;

            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;
        }

        private void rotate_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_rotate, "StateHidden", true);
            Focus();
        }

        // Scale
        public void resize_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_resize_advance, "StateVisible", true);
            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;
        }

        private void resize_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_resize_advance, "StateHidden", true);
            Focus();
        }

        private void info_toggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_object_information, "StateVisible", true);
            UI_object_information.Analyse(viewModel.SelectedModel);

            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
        }

        private void info_toggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(UI_object_information, "StateHidden", true);
            Focus();
        }

        public void remove_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;

            UI_move.slider_moveX.Minimum = -1000;
            UI_move.slider_moveX.Maximum = 1000;
            UI_move.slider_moveY.Minimum = -1000;
            UI_move.slider_moveY.Maximum = 1000;

            viewModel.DeleteModel();
            Focus();
        }

        private void zoomin_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;

            threeDControl.ZoomInKeyHandling(null, null);
            Focus();
        }

        private void zoomout_toggleButton_Click(object sender, RoutedEventArgs e)
        {
            view_toggleButton.IsChecked = false;
            move_toggleButton.IsChecked = false;
            rotate_toggleButton.IsChecked = false;
            resize_toggleButton.IsChecked = false;
            info_toggleButton.IsChecked = false;

            threeDControl.ZoomOutKeyHandling(null, null);
            Focus();
        }

        private void button_closeAbout_Click(object sender, RoutedEventArgs e)
        {
            gridAbout.Visibility = Visibility.Hidden;
        }

        void DebugLog()
        {
        }
    }
}
