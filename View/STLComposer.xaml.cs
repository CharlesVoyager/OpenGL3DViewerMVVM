using System.Windows;

namespace OpenGL3DViewerMVVM.View
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

        private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {   // When the selection changes, the Position usually changes as well. But keep threeDControl UpdateChanges() just in case that the Position does not change.
            MainWindow.main.threeDControl.UpdateChanges();
        }

        private void OnPositionPropertiesChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (MainWindow.main.viewModel.SelectedModel == null) return;

            MainWindow.main.viewModel.SelectedModel.UpdateTransMatrix();   // Bounding box will be automatically updated by position change.
            MainWindow.main.threeDControl.UpdateChanges();
            MainWindow.main.viewModel.SelectedModel.UpdateOutside();
        }

        private void OnRotatePropertiesChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (MainWindow.main.viewModel.SelectedModel == null) return;

            MainWindow.main.viewModel.SelectedModel.UpdateBoundingBoxAndMatrix();
            MainWindow.main.viewModel.SelectedModel.Land();
            MainWindow.main.threeDControl.UpdateChanges();
            MainWindow.main.viewModel.SelectedModel.UpdateOutside();
        }

        private void OnScalePropertiesChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (MainWindow.main.viewModel.SelectedModel == null) return;

            MainWindow.main.viewModel.SelectedModel.UpdateBoundingBoxAndMatrix();
            MainWindow.main.viewModel.SelectedModel.Land();
            MainWindow.main.threeDControl.UpdateChanges();
            MainWindow.main.viewModel.SelectedModel.UpdateOutside();
        }
    }
}
