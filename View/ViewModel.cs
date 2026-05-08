using OpenGL3DViewerMVVM.MeshIOLib;
using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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
        public RelayCommand CloneCommand => new RelayCommand(execute => CloneModel(), canExecute => SelectedModel != null);
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
                DoAutoScale(newModel);
            else if (isTooSmall(newModel.BoundingBox))
                check_stl_size_too_small(newModel);
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
                Autoposition(newModel);
            }
            else
            {
                newModel.Position.X = (float)newModel.BoundingBox.Center.x;
                newModel.Position.Y = (float)newModel.BoundingBox.Center.y;
                newModel.UpdateTransMatrix();
            }

            // Remember initial positions for all ViewModel.Models after Autoposition.
            foreach (var m in Models)
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

        public void CloneModel()
        {
            if (SelectedModel == null) return;
            ThreeDModel newModel = new ThreeDModel();
            SelectedModel?.CopyTo(newModel);
            Autoposition(newModel);
            newModel.UpdateOutOfBound();
            Models.Add(newModel);

            MainWindow.main.threeDControl.InvokeGL(() =>
            {
                newModel.Drawer.Init();
            });
        }
 
        bool Autoposition(ThreeDModel newModel)
        {
            List<ThreeDModel> allModels = new List<ThreeDModel>(Models);

            allModels.Add(newModel);

            float maxW = SettingsService.Instance.Settings.PrintAreaWidth;
            float maxH = SettingsService.Instance.Settings.PrintAreaDepth;

            if (allModels.Count == 1)
            {
                var model = allModels[0];
                model.Position.X = maxW / 2;
                model.Position.Y = maxH / 2;
                model.UpdateTransMatrix();
                return true;
            }

            var packer = new RectPacker(1, 1);
            var outPacker = new OutRectPacker(1000);
            int border = 1;
            float xOff = 0, yOff = 0;
            outPacker.SetPlatformSize(maxW, maxH);
            bool autosizeFailed = false;

            foreach (var stl in allModels)
            {
                int w = 2 * border + (int)Math.Ceiling(stl.xMax - stl.xMin);
                int h = 2 * border + (int)Math.Ceiling(stl.yMax - stl.yMin);
                if (!packer.addAtEmptySpotAutoGrow(new PackerRect(0, 0, w, h, stl), (int)maxW, (int)maxH))
                {
                    autosizeFailed = true;
                    outPacker.addOutsideSpotAutoGrow(new PackerRect(0, 0, w, h, stl));
                }
            }

            if (autosizeFailed)
            {
                float xCenter = (2000 - outPacker.w) / 2f;
                float yCenter = (2000 - outPacker.h) / 2f;
                float xOrigPos = xOff + xCenter + outPacker.vRects[0].x + border - 1000;
                float yOrigPos = yOff + yCenter + outPacker.vRects[0].y + border - 1000;
                for (int i = 1; i < outPacker.vRects.Count; i++)
                {
                    var s = (ThreeDModel)outPacker.vRects[i].obj;
                    s.Position.X += xOff + xCenter + outPacker.vRects[i].x + border - 1000 - xOrigPos - s.xMin;
                    s.Position.Y += yOff + yCenter + outPacker.vRects[i].y + border - 1000 - yOrigPos - s.yMin;
                    s.UpdateTransMatrix();
                }
                MessageBox.Show(Trans.T("M_PRINTER_BED_FULL_TEXT"),
                                Trans.T("W_PRINTER_BED_FULL"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }

            float xAdd = (maxW - packer.w) / 2f;
            float yAdd = (maxH - packer.h) / 2f;
            foreach (PackerRect rect in packer.vRects)
            {
                var s = (ThreeDModel)rect.obj;
                s.Position.X += xOff + xAdd + rect.x + border - s.xMin;
                s.Position.Y += yOff + yAdd + rect.y + border - s.yMin;
                s.UpdateTransMatrix();
            }
            return true;
        }

        public void check_stl_size_too_small(ThreeDModel model)
        {
            if (model == null) return;

            if (isTooSmall(model.BoundingBox))
            {
                var dlg = new ObjectResizeDialog(
                    model.BoundingBox.Size.x,
                    model.BoundingBox.Size.y,
                    model.BoundingBox.Size.z);
                if (MainWindow.main.Visibility == Visibility.Visible)
                    dlg.Owner = MainWindow.main;    // Ensure the dialog is on top of main window, otherwise user may miss the dialog and think the software is not responding.
                dlg.ShowDialog();
                if (dlg.gIsScale) DoAutoScale(model);
                else if (dlg.gIsInch) DoMmToInch(model);
                else model.Land();
            }
        }

        private bool AskUserToChangeUnit()
        {
            var sb = new StringBuilder(Trans.T("M_RESIZE_MODEL_TOO_BIG")).AppendLine()
                                                                         .Append(Trans.T("M_RESIZE_ASK_TO_SCALE_UP"));
            return System.Windows.MessageBox.Show(sb.ToString(),
                                   Trans.T("M_RESIZE_SCALE_UP_TITLE"),
                                   MessageBoxButton.YesNo,
                                   MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        // Auto scale the model to fit the half printer bed in X axis on the largest dimension.
        public void DoAutoScale(ThreeDModel model)
        {
            try
            {
                var bbox = model.BoundingBox;

                // Find the largest dimension of the model.
                double maxDim = Math.Max(Math.Max(bbox.Size.x, bbox.Size.y), bbox.Size.z);
                double scaleFactor = (SettingsService.Instance.Settings.PrintAreaWidth / 2) / maxDim;

                model.Scale.x = (float)scaleFactor;
                model.Scale.y = (float)scaleFactor;
                model.Scale.z = (float)scaleFactor;

                model.UpdateBoundingBoxAndMatrix();
                model.Land();
                model.UpdateOutOfBound();
                MainWindow.main.threeDControl.UpdateChanges();
            }
            catch { }
        }

        public void DoMmToInch(ThreeDModel? model = null)
        {
            if (model == null)
                model = SelectedModel;

            if (model == null) return;
            try
            {
                var ui = MainWindow.main.UI_resize_advance;
                ui.button_mmtoinch.IsEnabled = false;
                ui.button_inchtomm.IsEnabled = true;

                double tempX = model.BoundingBox.Size.x * 25.4, tempY = model.BoundingBox.Size.y * 25.4, tempZ = model.BoundingBox.Size.z * 25.4;
                if (tempX > SettingsService.Instance.Settings.PrintAreaWidth ||
                    tempY > SettingsService.Instance.Settings.PrintAreaDepth ||
                    tempZ > SettingsService.Instance.Settings.PrintAreaHeight)
                {
                    if (!AskUserToChangeUnit())
                    {
                        ui.button_mmtoinch.IsEnabled = true;
                        ui.button_inchtomm.IsEnabled = false;
                        return;
                    }
                }

                double scaleFactor = 25.4; // 1 inch = 25.4 mm

                model.Scale.x *= (float)scaleFactor;
                model.Scale.y *= (float)scaleFactor;
                model.Scale.z *= (float)scaleFactor;

                model.UpdateBoundingBoxAndMatrix();
                model.Land();
                model.UpdateOutOfBound();
                MainWindow.main.threeDControl.UpdateChanges();
            }
            catch { }
        }

        public void DoInchToMm(ThreeDModel? model = null)
        {
            if (model == null)
                model = SelectedModel;

            if (model == null) return;
            try
            {
                var ui = MainWindow.main.UI_resize_advance;
                ui.button_mmtoinch.IsEnabled = true;
                ui.button_inchtomm.IsEnabled = false;

                double scaleFactor = 25.4; // 1 inch = 25.4 mm

                model.Scale.x /= (float)scaleFactor;
                model.Scale.y /= (float)scaleFactor;
                model.Scale.z /= (float)scaleFactor;

                model.UpdateBoundingBoxAndMatrix();
                model.Land();
                model.UpdateOutOfBound();
                MainWindow.main.threeDControl.UpdateChanges();
            }
            catch { }
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
