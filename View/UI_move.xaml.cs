using System.Windows;
using System.Windows.Input;
using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;

#nullable disable

namespace OpenGL3DViewerMVVM.View
{
    /// <summary>
    /// Interaction logic for UI_move.xaml
    /// </summary>
    public partial class UI_move : System.Windows.Controls.UserControl
    {
        public UI_move()
        {
            InitializeComponent();
            try
            {
                if (MainWindow.main != null)
                    MainWindow.main.languageChanged += translate;
            }
            catch { }
        }

        private void translate()
        {
            button_move_reset.ToolTip = Trans.T("B_RESET");
            button_land.ToolTip = Trans.T("B_LAND");

            button_move_reset.Content = Trans.T("B_RESET");
            button_land.Content = Trans.T("B_LAND");
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (e.Text == "")
            {
                return;
            }

            char c = Convert.ToChar(e.Text);
            if (Char.IsNumber(c) || e.Text.Trim() == ".")
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
            if (e.Text.Trim() == ".")
            {
                if (moveX_textbox.IsFocused)
                {
                    if (moveX_textbox.Text.IndexOf(".") != -1)
                    {
                        e.Handled = true;
                    }
                }
                else if (moveY_textbox.IsFocused)
                {
                    if (moveY_textbox.Text.IndexOf(".") != -1)
                    {
                        e.Handled = true;
                    }
                }
                else if (moveZ_textbox.IsFocused)
                {
                    if (moveZ_textbox.Text.IndexOf(".") != -1)
                    {
                        e.Handled = true;
                    }
                }
            }
            base.OnPreviewTextInput(e);
        }

        public void SetSliderMinimumMaximum()
        {
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            double xShift = stl.BoundingBox.Center.x - stl.PositionX;
            double yShift = stl.BoundingBox.Center.y - stl.PositionY;
            double zShift = stl.BoundingBox.Center.z - stl.PositionZ;

            slider_moveX.Maximum = SettingsService.Instance.Settings.PrintAreaWidth - (stl.BoundingBox.Size.x / 2) - xShift;
            slider_moveX.Minimum = stl.BoundingBox.Size.x / 2 - xShift;
            slider_moveY.Maximum = SettingsService.Instance.Settings.PrintAreaDepth - (stl.BoundingBox.Size.y / 2) - yShift;
            slider_moveY.Minimum = stl.BoundingBox.Size.y / 2 - yShift;
            slider_moveZ.Maximum = SettingsService.Instance.Settings.PrintAreaHeight - (stl.BoundingBox.Size.z / 2) - zShift;
            slider_moveZ.Minimum = stl.BoundingBox.Size.z / 2 - zShift;

            slider_moveX.Maximum = Math.Floor(slider_moveX.Maximum * 10) * 0.1;
            slider_moveY.Maximum = Math.Floor(slider_moveY.Maximum * 10) * 0.1;
            slider_moveZ.Maximum = Math.Floor(slider_moveZ.Maximum * 10) * 0.1;

            slider_moveX.Minimum = Math.Ceiling(slider_moveX.Minimum * 10) * 0.1;
            slider_moveY.Minimum = Math.Ceiling(slider_moveY.Minimum * 10) * 0.1;
            slider_moveZ.Minimum = Math.Ceiling(slider_moveZ.Minimum * 10) * 0.1;
        }

        public void button_move_reset_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            stl.PositionX = stl.InitialPosition.x;
            stl.PositionY = stl.InitialPosition.y;
            stl.PositionZ = stl.InitialPosition.z;
        }

        public void button_land_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            stl.Land();

            // Land() will NOT trigger OnPropertyChanged(). Therefore, update slider values manually.
            slider_moveX.Value = stl.PositionX;
            slider_moveY.Value = stl.PositionY;
            slider_moveZ.Value = stl.PositionZ;
        }

        private void slider_moveX_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                slider_moveX.Value++;
            else
                slider_moveX.Value--;
            e.Handled = true;   
        }

        private void slider_moveY_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                slider_moveY.Value++;
            else
                slider_moveY.Value--;
            e.Handled = true;     
        }

        private void slider_moveZ_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                slider_moveZ.Value++;
            else
                slider_moveZ.Value--;
            e.Handled = true;   
        }

        private void moveX_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
                    if (stl == null) return;
                    slider_moveX.Value = Convert.ToDouble(moveX_textbox.Text);
                }
                catch { }
            }
        }

        private void moveY_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
                    if (stl == null) return;
                    slider_moveY.Value = Convert.ToDouble(moveY_textbox.Text);
                }
                catch { }
            }
        }

        private void moveZ_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
                    if (stl == null) return;
                    slider_moveZ.Value = Convert.ToDouble(moveZ_textbox.Text);
                }
                catch { }
            }
        }

        private void moveX_textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (moveX_textbox.Text.Trim() == "")
            {
                moveX_textbox.Text = slider_moveX.Value.ToString();
            }
        }

        private void moveY_textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (moveY_textbox.Text.Trim() == "")
            {
                moveY_textbox.Text = slider_moveY.Value.ToString();
            }
        }

        private void moveZ_textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (moveZ_textbox.Text.Trim() == "")
            {
                moveZ_textbox.Text = slider_moveZ.Value.ToString();
            }
        }

        private void OnSelectionChange(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SetSliderMinimumMaximum();
        }
    }
}