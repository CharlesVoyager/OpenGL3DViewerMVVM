using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;


#nullable disable

namespace OpenGL3DViewerMVVM.View
{
    public class UniformScaleToPercent : IValueConverter
    {
        // UniformScale value to slider percent value.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d * 100;
            return 100;
        }

        // Slider percent value to UniformScale value.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d / 100;
            return 1;
        }
    }

    public partial class UI_resize_advance : UserControl
    {
        const double MIN_DIMENSION = 0.001; // Minimum dimension to prevent exception when calculating scale.

        public UI_resize_advance()
        {
            InitializeComponent();
            try
            {
                button_mmtoinch.IsEnabled = true;
                button_inchtomm.IsEnabled = false;
                if (MainWindow.main != null)
                    MainWindow.main.languageChanged += translate;
            }
            catch { }
        }

        private void translate()
        {
            lbl_XUnits.Content = Trans.T("L_MM");
            lbl_YUnits.Content = Trans.T("L_MM");
            lbl_ZUnits.Content = Trans.T("L_MM");

            button_Reset.ToolTip = Trans.T("B_RESET");
            button_Reset.Content = Trans.T("B_RESET");
            lbl_Uniform.Content = Trans.T("L_UNIFORM");
            lbl_Size.Content = Trans.T("L_SIZE");
            btn_Scale.Content = Trans.T("B_APPLY");
            button_mmtoinch.Content = Trans.T("B_SCALE_UP") + " (" + Trans.T("L_MM") + "->" + Trans.T("L_INCH") + ")";
            button_inchtomm.Content = Trans.T("B_SCALE_DOWN") + " (" + Trans.T("L_INCH") + "->" + Trans.T("L_MM") + ")";
        }

        // Since text boxes are not binding to properties, update text boxes after SelectionModel is changed.
        private void OnSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            updateSliderMaximum();
        }

        private void sliderResizeTemp_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainWindow.main == null) return;
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            if (e.Delta > 0)
                stl.UniformScale += 0.01;
            else
                stl.UniformScale -= 0.01;
            e.Handled = true;
        }

        void updateSliderMaximum()
        {
            if (MainWindow.main == null) return;
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            // Ensure the value of dimension is not zero; otherwise, exception happens when calculating scale.
            double dimX = Math.Max(stl.Model.boundingBox.Size.x, MIN_DIMENSION);
            double dimY = Math.Max(stl.Model.boundingBox.Size.y, MIN_DIMENSION);
            double dimZ = Math.Max(stl.Model.boundingBox.Size.z, MIN_DIMENSION);

            double txMaxScalableValue = Convert.ToDouble(SettingsService.Instance.Settings.PrintAreaWidth) / dimX;
            double tyMaxScalableValue = Convert.ToDouble(SettingsService.Instance.Settings.PrintAreaDepth) / dimY;
            double tzMaxScalableValue = Convert.ToDouble(SettingsService.Instance.Settings.PrintAreaHeight) / dimZ;
            double tMaxScalableValue = Math.Min(Math.Min(txMaxScalableValue, tyMaxScalableValue), Math.Min(tyMaxScalableValue, tzMaxScalableValue));

            slider_resize.Maximum = tMaxScalableValue * 100;
        }

        public void button_Reset_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.main == null) return;
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            stl.UniformScale = 1;

            stl.IsUniformScale = true;
            button_mmtoinch.IsEnabled = true;
            button_inchtomm.IsEnabled = false;

            updateSliderMaximum();

            MainWindow.main.viewModel.check_stl_size_too_small(stl);
            stl.Land();
            MainWindow.main.threeDControl.UpdateChanges();

            updateSliderMaximum();
        }

        private void button_mmtoinch_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            button_mmtoinch.IsEnabled = false;
            button_inchtomm.IsEnabled = true;

            MainWindow.main.viewModel.DoMmToInch(model);
        }

        private void button_inchtomm_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            button_mmtoinch.IsEnabled = true;
            button_inchtomm.IsEnabled = false;

            MainWindow.main.viewModel.DoInchToMm(model);
        }

        private void btn_Scale_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            try
            {
                Double scaleValue = Convert.ToDouble(txt_Scale.Text) / 100;
                model.UniformScale = scaleValue;
            }
            catch { }
        }
    }
}
