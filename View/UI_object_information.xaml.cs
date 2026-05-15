using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.model.geom;

namespace OpenGL3DViewerMVVM.View
{
    /// <summary>
    /// Interaction logic for UI_object_information.xaml
    /// </summary>
    public partial class UI_object_information : System.Windows.Controls.UserControl
    {
        public UI_object_information()
        {
            InitializeComponent();

            try
            {
                if (MainWindow.main != null)
                    MainWindow.main.languageChanged += translate;
            }
            catch { }
        }

        void translate()
        {
        }

        void updateNonMVVMProperties(ThreeDModel pm)
        {
            //Stopwatch sw = Stopwatch.StartNew();

            if (pm == null || pm.Model == null)
            {
                txtVolume.Text = "0.000 cm³";
                txtSizeX.Text = "0.000 mm";
                txtSizeY.Text = "0.000 mm";
                txtSizeZ.Text = "0.000 mm";
                txtCollision.Text = "";
                return;
            }

            double volume = 0;
            foreach (TopoTriangle t in pm.Model.drawTriangles)
                volume += t.SignedVolume();

            volume = volume * pm.ScaleX * pm.ScaleY * pm.ScaleZ;

            RHBoundingBox bbox = pm.BoundingBox;

            string CubicCM = (0.001 * Math.Abs(volume)).ToString("0.000");
            if (CubicCM == "0.000")
            { CubicCM = "0.001"; }

            txtVolume.Text = CubicCM + " cm³";
            txtSizeX.Text = bbox.Size.x.ToString("0.000") + " mm";
            txtSizeY.Text = bbox.Size.y.ToString("0.000") + " mm";
            txtSizeZ.Text = bbox.Size.z.ToString("0.000") + " mm";

            txtCollision.Text = pm.Outside.ToString();

            //Debug.WriteLine("Elapsed time for Analyse: " + sw.ElapsedMilliseconds + " ms");
        }

        private void OnSelectionChange(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (State1Panel.Opacity == 0) return;

            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            updateNonMVVMProperties(model);
        }

        private void OnStateVisibleCompleted(object sender, EventArgs e)
        {
            ThreeDModel model = MainWindow.main.viewModel.SelectedModel;
            if (model == null) return;

            Console.WriteLine(State1Panel.Opacity);
            updateNonMVVMProperties(model);
        }
    }
}
