using OpenGL3DViewerMVVM.ModelLib.Utils;

namespace OpenGL3DViewerMVVM.View
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
                MainWindow.main.languageChanged += translate;
        }

        private void translate()
        {
            txtWarningMsg.Text = Trans.T("L_OUT_OF_BOUNDARY");
        }
    }
}
