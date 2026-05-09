using OpenGL3DViewerMVVM.ModelLib.Utils;

namespace View3D.view
{
    /// <summary>
    /// Interaction logic for OutofBound.xaml
    /// </summary>
    public partial class OutofBound : System.Windows.Controls.UserControl
    {
        public OutofBound()
        {
            InitializeComponent();

            if ( MainWindow.main != null)  // This check is necessary for XAML designer to avoid null reference exceptions.
            {
                MainWindow.main.languageChanged += translate;
                DataContext = MainWindow.main.viewModel;
            }
        }

        private void translate()
        {
            txt_WarningMsg.Text = Trans.T("L_OUT_OF_BOUNDARY");
        }
    }
}
