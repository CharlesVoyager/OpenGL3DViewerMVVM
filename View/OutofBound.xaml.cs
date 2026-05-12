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

            if (MainWindow.main != null)  // This check is necessary for XAML designer to avoid null reference exceptions.
            {
                MainWindow.main.languageChanged += translate;
                DataContext = MainWindow.main.viewModel;
            }
            Visibility = System.Windows.Visibility.Collapsed;  // Initially hide the warning message.
        }

        private void translate()
        {
            txtWarningMsg.Text = Trans.T("L_OUT_OF_BOUNDARY");
        }

        private void Outside_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Visibility = System.Windows.Visibility.Visible;
        }

        private void Outside_Uncheked(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var m in MainWindow.main.viewModel.Models)
            {
                if (m.Outside)
                {
                    Visibility = System.Windows.Visibility.Visible;
                    return;
                }
            }
            Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
