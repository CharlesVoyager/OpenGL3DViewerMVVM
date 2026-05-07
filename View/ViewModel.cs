using OpenGL3DViewerMVVM.MeshIOLib;
using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using View3D;
using View3D.model.geom;
using View3D.view;

namespace OpenGL3DViewerMVVM.View
{
    public class ViewModel : ViewModelBase
    {
        public ObservableCollection<ThreeDModel> Models { get; set; }

        public RelayCommand AddCommand => new RelayCommand(execute => AddModel());
        public RelayCommand DeleteCommand => new RelayCommand(execute => DeleteModel(), canExecute => SelectedModel != null);

        public ViewModel()
        {
            Models = new ObservableCollection<ThreeDModel>();
        }

        private ThreeDModel? selectedModel;

        public ThreeDModel? SelectedModel
        {
            get => selectedModel;
            set
            {
                if (selectedModel != value)
                {
                    if (selectedModel != null)
                        selectedModel.Selected = false;

                    selectedModel = value;

                    if (selectedModel != null)
                        selectedModel.Selected = true;

                    OnPropertyChanged();
                    MainWindow.main.threeDControl.UpdateChanges();
                }
                MainWindow.main.setbuttonVisable(selectedModel != null);
            }
        }

        // Check if all models are in print bed area.
        public bool IsOutOfBound
        {
            get
            {
                foreach (var m in Models)
                {
                    if (m.Outside)
                        return true;
                }
                return false;
            }
        }

        bool _isLoadingModel = false;
        public bool IsLoadingModel 
        {
            get { return _isLoadingModel; }
            set
            {
                _isLoadingModel = value;
                OnPropertyChanged(nameof(IsLoadingModel));
            }
        }

        int _loadModelProgress = 0;
        public int LoadModelProgress 
        { 
            get { return _loadModelProgress; }
            set
            {
                _loadModelProgress = value;
                OnPropertyChanged(nameof(LoadModelProgress));
            }
        }

        public void UpdateOutOfBound() => OnPropertyChanged(nameof(IsOutOfBound));

        bool isTooSmall(RHBoundingBox boundingBox)
        {
            // Don't use z size here because some STL files may have very small z size but large x/y size, and they should not be considered as "too small".
            return (boundingBox.Size.x < 10 && boundingBox.Size.y < 10 && boundingBox.Size.z < 10);
        }

        bool isTooBig(RHBoundingBox boundingBox)
        {
            return (boundingBox.Size.x - 1e-4 > SettingsService.Instance.Settings.PrintAreaWidth) ||
                   (boundingBox.Size.y - 1e-4 > SettingsService.Instance.Settings.PrintAreaDepth) ||
                   (Math.Floor(boundingBox.Size.z * 1000) / 1000 > SettingsService.Instance.Settings.PrintAreaHeight);
        }

        public static readonly ManualResetEventSlim _meshDataReady = new ManualResetEventSlim(true);
        public async void AddModel(string? file = null)
        {
            if (file == null)
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

                openFileDialog.Title = "Select a File";
                openFileDialog.Filter = "3D Files (*.stl;*.glb)|*.stl;*.glb|" +
                                            "STL Files (*.stl)|*.stl|" +
                                            "GLB Files (*.glb)|*.glb";
                bool? resultDlg = openFileDialog.ShowDialog();

                if (resultDlg == true)
                    file = openFileDialog.FileName;
                else
                    return;
            }

            ThreeDModel newModel = new ThreeDModel();
            bool modelToLand = true;
            var modelIO = new MeshIOWrapper();

            IsLoadingModel = true;
            _meshDataReady.Reset();
            // Offload heavy work to background thread — UI thread is free immediately
            await Task.Run(() =>
            {
                try
                {           
                    modelIO.LoadWOCatch(file, newModel.Model);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error: " + Trans.T("M_LOAD_FILE_FAIL"));
                    return;
                }

                // NOTES:
                // 1. Model (TopoModel): Original STL file triangles data.
                // 2. Mesh (Submesh): Centerized triangles data. 
                newModel.ModelToMesh();

                // NOTES:
                // 1. Auto position and checking model size need bounding box information.
                // 2. Current bounding box is for orignal STL data. 
                newModel.CopyTopoModelBoundingBoxToPrintModel();

                _meshDataReady.Set();
                Console.WriteLine("LoadWOCatch Done.");
            });
            IsLoadingModel = false;
            if (_meshDataReady.Wait(0) == false)// It means some expection happens when loading a STL file.
            {
                _meshDataReady.Set();
                return;
            }
            if (newModel.Model.drawTriangles.Count == 0)
            {
                newModel.Model.Clear();
                return;
            }
            newModel.Name = Path.GetFileName(file);

            if (isTooSmall(newModel.BoundingBox) && newModel.Name.Contains(".glb"))
                MainWindow.main.stlComposer.DoAutoScale(newModel);
            else if (isTooSmall(newModel.BoundingBox))
                MainWindow.main.stlComposer.check_stl_size_too_small(newModel);
            else if (isTooBig(newModel.BoundingBox))  // the object is too big.
            {
                double tXBound = newModel.BoundingBox.Size.x / SettingsService.Instance.Settings.PrintAreaWidth;
                double tYBound = newModel.BoundingBox.Size.y / SettingsService.Instance.Settings.PrintAreaDepth;
                double tZBound = newModel.BoundingBox.Size.z / SettingsService.Instance.Settings.PrintAreaHeight;
                double tMax = Math.Max(Math.Max(tXBound, tYBound), Math.Max(tYBound, tZBound));
                double scaleValue = 0;

                if (tMax == tXBound) scaleValue = SettingsService.Instance.Settings.PrintAreaWidth / newModel.BoundingBox.Size.x;
                else if (tMax == tYBound) scaleValue = SettingsService.Instance.Settings.PrintAreaDepth / newModel.BoundingBox.Size.y;
                else if (tMax == tZBound) scaleValue = SettingsService.Instance.Settings.PrintAreaHeight / newModel.BoundingBox.Size.z;

                var result = MessageBox.Show(
                    Trans.T("M_OBJ_SCALE_DOWN") + " " + (int)(scaleValue * 100) + "%",
                    Trans.T("W_OBJ_TOO_LARGE"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        newModel.Scale.x = newModel.Scale.y = newModel.Scale.z = scaleValue;
                        newModel.UpdateBoundingBoxAndMatrix();
                        newModel.Land();
                    }
                    catch { }
                }
            }
            else
            {
                newModel.UpdateBoundingBoxAndMatrix();
            }

            newModel.Position.Z = newModel.BoundingBox.Size.z / 2;
            if (modelToLand)
            {
                MainWindow.main.stlComposer.Autoposition(newModel);
            }
            else
            {
                newModel.Position.X = (float)newModel.BoundingBox.Center.x;
                newModel.Position.Y = (float)newModel.BoundingBox.Center.y;
                newModel.UpdateTransMatrix();
            }

            // Remember initial positions for all ViewModel.Models after Autoposition.
            foreach (var m in MainWindow.main.viewModel.Models)
            {
                m.InitialPosition.x = m.Position.X;
                m.InitialPosition.y = m.Position.Y;
                m.InitialPosition.z = m.Position.Z;
            }

            newModel.InitialPosition.x = newModel.Position.X;
            newModel.InitialPosition.y = newModel.Position.Y;
            newModel.InitialPosition.z = newModel.Position.Z;

            Models.Add(newModel);
            SelectedModel = newModel;

            MainWindow.main.threeDControl.InvokeGL(() =>
            {
                newModel.Drawer.Init();
                MainWindow.main.threeDControl.UpdateChanges();
            });
        }

        public void DeleteModel()
        {
            if (SelectedModel != null)
                Models.Remove(SelectedModel);
        }
    }

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object? parameter) => canExecute == null || canExecute(parameter);
        public void Execute(object? parameter) => execute(parameter);
    }
}
