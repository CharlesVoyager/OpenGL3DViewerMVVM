using OpenGL3DViewerMVVM.ModelLib.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace View3D.view
{
    /// <summary>
    /// Interaction logic for BusyWindow.xaml
    /// </summary>
    public partial class BusyWindow : System.Windows.Controls.UserControl
    {
        public event EventHandler? AbortTask;

        DispatcherTimer? timer;
        Stopwatch? stopWatch;

        public BusyWindow()
        {
            InitializeComponent();

            if (MainWindow.main != null)
                MainWindow.main.languageChanged += translate;

            stopWatch = new Stopwatch();

            timer = new DispatcherTimer();
            timer.Tick += dispatcherTimerTick;
            timer.Interval = TimeSpan.FromMilliseconds(1000);

            if (MainWindow.main != null)
                DataContext = MainWindow.main.viewModel;
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            if (isVisible)
            {
                if (stopWatch == null || timer == null) return;

                textBlock_time.Text = "00:00:00";
                stopWatch.Reset();
                stopWatch.Start();
                timer.Start();
            }
            else
            {
                if (stopWatch == null || timer == null) return;

                textBlock_time.Text = "00:00:00";
                stopWatch.Stop();
                timer.Stop();
            }
        }

        private void translate()
        {
            labelElapsedTime.Text = Trans.T("L_ELAPSED_TIME");
        }

        public int getStopWatch()
        {
            if (stopWatch == null) return 0;
            return Convert.ToInt16(stopWatch.Elapsed.Seconds);
        }

        private void dispatcherTimerTick(object? sender, EventArgs e)
        {
            if (stopWatch == null) return;

            textBlock_time.Text =   stopWatch.Elapsed.Hours.ToString("00")
                            + ":" + stopWatch.Elapsed.Minutes.ToString("00")
                            + ":" + stopWatch.Elapsed.Seconds.ToString("00");

        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (AbortTask != null)
                AbortTask(this, new EventArgs());
        }
    }
}
