using OpenGL3DViewerMVVM.MeshIOLib;
using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using OpenGL3DViewerMVVM.model.geom;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace OpenGL3DViewerMVVM.View
{
    public class ViewModel : ViewModelBase
    {
        public ObservableCollection<ThreeDModel> Models { get; set; }

        public RelayCommand AddCommand => new RelayCommand(execute => AddModel());
        public RelayCommand DeleteCommand => new RelayCommand(execute => DeleteModel(), canExecute => SelectedModel != null);
        public RelayCommand CloneCommand => new RelayCommand(execute => CloneModel(), canExecute => SelectedModel != null);
        public RelayCommand ResetCommand => new RelayCommand(execute => ResetModel(), canExecute => SelectedModel != null);
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

                    MainWindow.main.threeDControl.UpdateChanges();  //This is needed when selecting a model from the list in STLComposer.
                    OnPropertyChanged();
                }
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

        bool isTooSmall(RHBoundingBox boundingBox)
        {
            return (boundingBox.Size.x < 10 && boundingBox.Size.y < 10 && boundingBox.Size.z < 10);
        }

        bool isTooBig(RHBoundingBox boundingBox)
        {
            return (boundingBox.Size.x - 1e-4 > SettingsService.Instance.Settings.PrintAreaWidth) ||
                   (boundingBox.Size.y - 1e-4 > SettingsService.Instance.Settings.PrintAreaDepth) ||
                   (Math.Floor(boundingBox.Size.z * 1000) / 1000 > SettingsService.Instance.Settings.PrintAreaHeight);
        }

        public static readonly ManualResetEventSlim _meshDataReady = new ManualResetEventSlim(true);
        public async void AddModel(string? file = null, bool isAutoPosition = true)
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
                double scaleValue = Math.Floor( (1 / tMax) * 100) / 100;

                var result = MessageBox.Show(
                    Trans.T("M_OBJ_SCALE_DOWN") + " " + (int)(scaleValue * 100) + "%",
                    Trans.T("W_OBJ_TOO_LARGE"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        newModel.UniformScale = scaleValue;
                    }
                    catch { }
                }
            }
            else
            {
                newModel.UpdateBoundingBoxAndMatrix();
            }

            newModel.PositionZ = newModel.BoundingBox.Size.z / 2;
            if (isAutoPosition)
            {
                Autoposition(newModel);
            }
            else
            {
                newModel.PositionX = (float)newModel.BoundingBox.Center.x;
                newModel.PositionY = (float)newModel.BoundingBox.Center.y;
            }

            Models.Add(newModel);
            SelectedModel = newModel;

            // Remember initial positions for all ViewModel.Models after Autoposition.
            foreach (var m in Models)
            {
                m.InitialPosition.x = m.PositionX;
                m.InitialPosition.y = m.PositionY;
                m.InitialPosition.z = m.PositionZ;
            }

            MainWindow.main.threeDControl.InvokeGL(() =>
            {
                newModel.Drawer.Init();
                MainWindow.main.threeDControl.UpdateChanges();
            });
        }

        public void DeleteModel()
        {
            if (SelectedModel == null) return;
            SelectedModel.Dispose();
            Models.Remove(SelectedModel);
        }

        public void CloneModel()
        {
            if (SelectedModel == null) return;
            ThreeDModel newModel = new ThreeDModel();
            SelectedModel?.CopyTo(newModel);
            Autoposition(newModel);
            newModel.UpdateOutside();
            Models.Add(newModel);

            MainWindow.main.threeDControl.InvokeGL(() =>
            {
                newModel.Drawer.Init();
            });
        }

        public void ResetModel()
        {
            if (SelectedModel == null) return;
            SelectedModel.Reset();
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
                model.PositionX = maxW / 2;
                model.PositionY = maxH / 2;
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
                    s.PositionX += xOff + xCenter + outPacker.vRects[i].x + border - 1000 - xOrigPos - s.xMin;
                    s.PositionY += yOff + yCenter + outPacker.vRects[i].y + border - 1000 - yOrigPos - s.yMin;
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
                s.PositionX += xOff + xAdd + rect.x + border - s.xMin;
                s.PositionY += yOff + yAdd + rect.y + border - s.yMin;
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
                model.UniformScale = scaleFactor;
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

                double tempX = model.BoundingBox.Size.x * 25.4, tempY = model.BoundingBox.Size.y * 25.4, tempZ = model.BoundingBox.Size.z * 25.4;
                if (tempX > SettingsService.Instance.Settings.PrintAreaWidth ||
                    tempY > SettingsService.Instance.Settings.PrintAreaDepth ||
                    tempZ > SettingsService.Instance.Settings.PrintAreaHeight)
                {
                    if (!AskUserToChangeUnit())
                        return;
                }

                double scaleFactor = 25.4; // 1 inch = 25.4 mm
                model.UniformScale = scaleFactor;
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

                double scaleFactor = 25.4; // 1 inch = 25.4 mm
                model.UniformScale /= scaleFactor;
                MainWindow.main.threeDControl.UpdateChanges();
            }
            catch { }
        }
    }
}
