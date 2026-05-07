using System.Windows;

namespace View3D.view
{
    public partial class STLComposer : Window
    {
        // ── Constructor ───────────────────────────────────────────────────────
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
