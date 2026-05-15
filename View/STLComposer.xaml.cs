using System.Windows;

namespace OpenGL3DViewerMVVM.View
{
    public partial class STLComposer : Window
    {
        public STLComposer()
        {
            InitializeComponent();
 
            if (MainWindow.main != null)
            {
                MainWindow.main.languageChanged += translate;
                DataContext = MainWindow.main.viewModel;
            }
        }

        public void translate() { }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Prevent the window from actually closing
            this.Hide();
        }
    }
}
