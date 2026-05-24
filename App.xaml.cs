using OpenGL3DViewerMVVM.ModelLib.Utils;
using OpenGL3DViewerMVVM.View;
using System.Globalization;
using System.IO;

#nullable disable

namespace OpenGL3DViewerMVVM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Initial Trans.T().
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);
            Trans trans = new Trans(AppDomain.CurrentDomain.BaseDirectory + "Resources");

            // OpenTK GameWindow runs on the main thread (required by GLFW)
            ThreeDControl threeDCtrl = new ThreeDControl(
                SettingsService.Instance.Settings.InitialClientSizeWidth, 
                SettingsService.Instance.Settings.InitialClientSizeHeight);

            // Launch WPF on a dedicated STA background thread
            MainWindow mainWindow = null;
            var wpfThread = new Thread(() =>
            {
                var app = new App();
                app.InitializeComponent();  // Loads App.xaml resources (global styles, etc.)
                mainWindow = new MainWindow(threeDCtrl);
                app.Run(mainWindow);  // WPF message pump runs here
            });
            wpfThread.SetApartmentState(ApartmentState.STA);
            wpfThread.IsBackground = false;
            wpfThread.Name = "OpenGL 3D Viewer WPF Thread";
            wpfThread.Start();

            // Wait until MainWindow is ready before starting OpenTK
            OpenGL3DViewerMVVM.MainWindow._mainWindowReady.Wait();

            // viewModel is created in MainWindow's constructor, so it is ready to use at this point.
            mainWindow.threeDControl.SubscribeToViewModel(mainWindow.viewModel);

            // threeDCamera is created in MainWindow's constructor, so it is ready to use at this point.
            mainWindow.threeDControl.SetCamera(mainWindow.threeDCamera);

            // Set camera to isometric view.
            mainWindow.threeDCamera.OnIsometricView();

            // Force the WPF MainWindow to the foreground after OpenTK's GLFW window
            // has been created — GLFW steals focus when its native Win32 window appears.
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.Topmost = true;   // momentarily force to top
                mainWindow.Activate();
                mainWindow.Focus();
                mainWindow.Topmost = false;  // restore normal z-order
            });

            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await ProcessCommandLine(mainWindow);
            });

            // Wait until STL model data is ready if import STL file through command line before starting rendering loop
            // NOTE: ProcessCommandline is invoked asynchronously (await). It will hit here before the command line processing is done.
            ViewModel._meshDataReady.Wait();

            // Blocks main thread for lifetime of GL window — correct!
            mainWindow.threeDControl.Run();

            Console.WriteLine("Exit the program.");
        }

        // Command line argument example: @"..\..\..\Stl\10_10_10.stl"
        private static async Task ProcessCommandLine(MainWindow main)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                string file = args[i];
                if (File.Exists(file))
                    await main.viewModel.ExecuteAddAsync(file);
            }
        }
    }
}
