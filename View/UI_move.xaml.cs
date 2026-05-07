using System.Windows;
using System.Windows.Input;
using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;

#nullable disable

namespace View3D.view
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
                {
                    MainWindow.main.languageChanged += translate;
                    DataContext = MainWindow.main.viewModel;
                }
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

        public void Initial()
        {
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            slider_moveX.ValueChanged -= slider_moveX_ValueChanged;
            slider_moveY.ValueChanged -= slider_moveY_ValueChanged;
            slider_moveZ.ValueChanged -= slider_moveZ_ValueChanged;

            slider_moveX.Maximum = SettingsService.Instance.Settings.PrintAreaWidth - (stl.BoundingBox.Size.x / 2);
            slider_moveX.Minimum = stl.BoundingBox.Size.x / 2;
            slider_moveY.Maximum = SettingsService.Instance.Settings.PrintAreaDepth - (stl.BoundingBox.Size.y / 2);
            slider_moveY.Minimum = stl.BoundingBox.Size.y / 2;
            slider_moveZ.Maximum = SettingsService.Instance.Settings.PrintAreaHeight - (stl.BoundingBox.Size.z / 2);
            slider_moveZ.Minimum = stl.BoundingBox.Size.z / 2;

            slider_moveX.ValueChanged += slider_moveX_ValueChanged;
            slider_moveY.ValueChanged += slider_moveY_ValueChanged;
            slider_moveZ.ValueChanged += slider_moveZ_ValueChanged;
        }

        public void button_move_reset_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            slider_moveX.Value = stl.InitialPosition.x;
            slider_moveY.Value = stl.InitialPosition.y;
            slider_moveZ.Value = stl.InitialPosition.z;
        }

        public void button_land_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            stl.Land();
            stl.UpdateOutOfBound();
            MainWindow.main.threeDControl.UpdateChanges();

            slider_moveX.Value = stl.Position.X;
            slider_moveY.Value = stl.Position.Y;
            slider_moveZ.Value = stl.Position.Z;
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

        private void slider_moveX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;
            model.PositionX = slider_moveX.Value;
        }

        private void slider_moveY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;
            model.PositionY = slider_moveY.Value;
        }

        private void slider_moveZ_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;
            model.PositionZ = slider_moveZ.Value;
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
            Initial();
        }
    }
}