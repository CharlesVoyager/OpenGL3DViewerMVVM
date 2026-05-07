using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#nullable disable

namespace View3D.view
{
    /// <summary>
    /// Interaction logic for UI_rotate.xaml
    /// </summary>
    /// 
    public partial class UI_rotate : System.Windows.Controls.UserControl
    {
        public bool partBuildInProgress = false;

        public UI_rotate()
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
            button_rotate_reset.ToolTip = Trans.T("B_RESET");
            button_rotate_reset.Content = Trans.T("B_RESET");
        }

        private double ConvertPositionAngel(System.Windows.Point soucePoint, System.Windows.Point targetPoint)
        {
            var res = (Math.Atan2(targetPoint.Y - soucePoint.Y, targetPoint.X - soucePoint.X)) / Math.PI * 180.0;
            res = (int)res;
            return (res >= 0 && res <= 180) ? res += 90 : ((res < 0 && res >= -90) ? res += 90 : res += 450);
        }

        private void StackPanelX_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StackPanelX_MouseMove(sender, e);
        }

        private void StackPanelX_MouseMove(object sender, MouseEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            labelX.Visibility = Visibility.Hidden;
            textRotX.Visibility = Visibility.Visible;
            if (e.LeftButton == MouseButtonState.Pressed && textRotX.IsMouseDirectlyOver == false)
                model.RotationX = ConvertPositionAngel(new Point(stackpanelX.Width / 2, stackpanelX.Height / 2), e.GetPosition(stackpanelX));
        }

        private void StackPanelY_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StackPanelY_MouseMove(sender, e);
        }

        private void StackPanelY_MouseMove(object sender, MouseEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            labelY.Visibility = Visibility.Hidden;
            textRotY.Visibility = Visibility.Visible;
            if (e.LeftButton == MouseButtonState.Pressed && textRotY.IsMouseDirectlyOver == false)
                model.RotationY = ConvertPositionAngel(new Point(stackpanelY.Width / 2, stackpanelY.Height / 2), e.GetPosition(stackpanelY));
        }

        private void StackPanelZ_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StackPanelZ_MouseMove(sender, e);
        }

        private void StackPanelZ_MouseMove(object sender, MouseEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            labelZ.Visibility = Visibility.Hidden;
            textRotZ.Visibility = Visibility.Visible;
            if (e.LeftButton == MouseButtonState.Pressed && textRotZ.IsMouseDirectlyOver == false)
                model.RotationZ = ConvertPositionAngel(new Point(stackpanelZ.Width / 2, stackpanelZ.Height / 2), e.GetPosition(stackpanelZ));
        }

        public void button_rotate_reset_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            model.RotationX = 0;
            model.RotationY = 0;
            model.RotationZ = 0;
        }

        private void stackpanelX_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (textRotX.IsFocused == true)
                return;
            textRotX.Visibility = Visibility.Hidden;
            labelX.Visibility = Visibility.Visible;
        }

        private void stackpanelY_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (textRotY.IsFocused == true)
                return;
            textRotY.Visibility = Visibility.Hidden;
            labelY.Visibility = Visibility.Visible;
        }

        private void stackpanelZ_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (textRotZ.IsFocused == true)
                return;
            textRotZ.Visibility = Visibility.Hidden;
            labelZ.Visibility = Visibility.Visible;
        }

        private void orangeX_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                textRotX.Text = (Convert.ToDouble(textRotX.Text) + 1).ToString();
            else
                textRotX.Text = (Convert.ToDouble(textRotX.Text) - 1).ToString();
            e.Handled = true;
        }

        private void orangeY_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                textRotY.Text = (Convert.ToDouble(textRotY.Text) + 1).ToString();
            else
                textRotY.Text = (Convert.ToDouble(textRotY.Text) - 1).ToString();
            e.Handled = true;
        }

        private void orangeZ_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                textRotZ.Text = (Convert.ToDouble(textRotZ.Text) + 1).ToString();
            else
                textRotZ.Text = (Convert.ToDouble(textRotZ.Text) - 1).ToString();
            e.Handled = true;
        }

        private void textboxX_LostFocus(object sender, RoutedEventArgs e)
        {
            textRotX.Visibility = Visibility.Hidden;
            labelX.Visibility = Visibility.Visible;
        }

        private void textboxY_LostFocus(object sender, RoutedEventArgs e)
        {
            textRotY.Visibility = Visibility.Hidden;
            labelY.Visibility = Visibility.Visible;
        }

        private void textboxZ_LostFocus(object sender, RoutedEventArgs e)
        {
            textRotZ.Visibility = Visibility.Hidden;
            labelZ.Visibility = Visibility.Visible;
        }

        private void textboxX_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                textRotX.Text = (Convert.ToDouble(textRotX.Text) + 1).ToString();
            }
            else if (e.Key == Key.Down)
            {
                textRotX.Text = (Convert.ToDouble(textRotX.Text) - 1).ToString();
            }
            else if (e.Key == Key.Enter)
            {
            }
        }

        private void textboxY_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                textRotY.Text = (Convert.ToDouble(textRotY.Text) + 1).ToString();
            }
            else if (e.Key == Key.Down)
            {
                textRotY.Text = (Convert.ToDouble(textRotY.Text) - 1).ToString();
            }
            else if (e.Key == Key.Enter)
            {
            }
        }

        private void textboxZ_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                textRotZ.Text = (Convert.ToDouble(textRotZ.Text) + 1).ToString();
            }
            else if (e.Key == Key.Down)
            {
                textRotZ.Text = (Convert.ToDouble(textRotZ.Text) - 1).ToString();
            }
            else if (e.Key == Key.Enter)
            {
            }
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
                if (textRotX.IsFocused)
                {
                    if (textRotX.Text.IndexOf(".") != -1)
                    {
                        e.Handled = true;
                    }
                }
                else if (textRotY.IsFocused)
                {
                    if (textRotY.Text.IndexOf(".") != -1)
                    {
                        e.Handled = true;
                    }
                }
                else if (textRotZ.IsFocused)
                {
                    if (textRotZ.Text.IndexOf(".") != -1)
                    {
                        e.Handled = true;
                    }
                }
            }
            base.OnPreviewTextInput(e);
        }

        // Input angle must between 0~360 degree
        private void limitRotateAngle(System.Windows.Controls.TextBox textbox)
        {
            if (Convert.ToDouble(textbox.Text) >= 360 || textbox.Text.Length > 3)
            {
                textbox.Text = "360";
            }
            else if (Convert.ToDouble(textbox.Text) <= 0)
            {
                textbox.Text = "0";
            }
        }

        private void OnSelectionChange(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

