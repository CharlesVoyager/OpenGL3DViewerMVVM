using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;

#nullable disable

namespace OpenGL3DViewerMVVM.View
{
    enum Axis
    {
        X,
        Y,
        Z
    }

    public partial class UI_resize_advance : UserControl
    {
        const double MIN_DIMENSION = 0.001; // Minimum dimension to prevent exception when calculating scale.

        bool gIsShow = false;

        public UI_resize_advance()
        {
            InitializeComponent();
            try
            {
                button_mmtoinch.IsEnabled = true;
                button_inchtomm.IsEnabled = false;
                slider_resize.Minimum = 1;  // NOTE: The value of resize cannot be zero; otherwise, exception happens.
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
            updateSliderMinMaxValue();
        }

        private void slider_resize_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainWindow.main == null) return;
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            if (e.Delta > 0)
                slider_resize.Value += 0.01;
            else
                slider_resize.Value -= 0.01;
            e.Handled = true;
        }

        // NOTE: Slider change is always changed in uniform scale.
        private void slider_resize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.main == null) return;
            if (gIsShow == true) return;
            ThreeDModel stl = MainWindow.main.viewModel.SelectedModel;
            if (stl == null) return;

            stl.UniformScale = slider_resize.Value / 100;
        }

        void updateSliderMinMaxValue()
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

            slider_resize.ValueChanged -= slider_resize_ValueChanged;
            slider_resize.Maximum = tMaxScalableValue * 100;
            slider_resize.Value = stl.UniformScale * 100;
            slider_resize.ValueChanged += slider_resize_ValueChanged;
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

            gIsShow = true;
            slider_resize.Value = 100;
            gIsShow = false;
            updateSliderMinMaxValue();

            MainWindow.main.viewModel.check_stl_size_too_small(stl);
            stl.Land();
            MainWindow.main.threeDControl.UpdateChanges();

            gIsShow = true;
            slider_resize.Value = stl.ScaleX * 100;
            gIsShow = false;
            updateSliderMinMaxValue();
        }

        private void button_mmtoinch_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            button_mmtoinch.IsEnabled = false;
            button_inchtomm.IsEnabled = true;

            MainWindow.main.viewModel.DoMmToInch(model);

            slider_resize.ValueChanged -= slider_resize_ValueChanged;
            slider_resize.Value = model.ScaleX * 100;
            slider_resize.ValueChanged += slider_resize_ValueChanged;
        }

        private void button_inchtomm_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            button_mmtoinch.IsEnabled = true;
            button_inchtomm.IsEnabled = false;

            MainWindow.main.viewModel.DoInchToMm(model);
            
            slider_resize.ValueChanged -= slider_resize_ValueChanged;
            slider_resize.Value = model.ScaleX * 100;
            slider_resize.ValueChanged += slider_resize_ValueChanged;
        }

        private void btn_Scale_Click(object sender, RoutedEventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            try
            {
                Double scaleValue = slider_resize.Value / 100;
                model.UniformScale = scaleValue;
            }
            catch { }
        }
    }
}
