using OpenGL3DViewerMVVM.MeshIOLib;
using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using OpenGL3DViewerMVVM.View;
using System.IO;
using System.Text;
using System.Windows;
using View3D.model.geom;

#nullable disable

namespace View3D.view
{
    public partial class STLComposer : Window
    {
        public STLComposerViewModel ViewModel;

        // ── Constructor ───────────────────────────────────────────────────────
        public STLComposer()
        {
            InitializeComponent();
 
            try
            {
                if (MainWindow.main != null)
                    MainWindow.main.languageChanged += translate;
            }
            catch { }

            ViewModel = new STLComposerViewModel();
            DataContext = ViewModel;
        }

        public void translate() { }

        public List<ThreeDModel> GetAllPrintModels()
        {
            var list = new List<ThreeDModel>();
            foreach (var m in ViewModel.Models)
                if (IsValidPrintModel(m)) list.Add(m);
            return list;
        }

        public List<ThreeDModel> GetSelectedPrintModels()
        {
            var list = new List<ThreeDModel>();
            foreach (var m in ViewModel.Models)
                if (IsValidPrintModel(m) && m.Selected) list.Add(m);
            return list;
        }

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

        public async void OpenAndAddObject(string file)
        {
            if (MainWindow.main == null) return;

            ThreeDModel newModel = new ThreeDModel();
            bool modelToLand    = true;
            var  modelIO        = new MeshIOWrapper();
            MainWindow.main.BusyWindow.EnableBusyWindow();
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
                // 1. Auto position needs bounding box information.
                // 2. Current bounding box is for orignal STL data. 
                newModel.CopyTopoModelBoundingBoxToPrintModel();

                _meshDataReady.Set();
                Console.WriteLine("LoadWOCatch Done.");
            });
            MainWindow.main.BusyWindow.DisableBusyWindow();
            if (_meshDataReady.Wait(0) == false)// It means some expection happens when loading a STL file.
            {
                _meshDataReady.Set();
                return; 
            }
            if (MainWindow.main.BusyWindow.killed || newModel.Model.drawTriangles.Count == 0)
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
            foreach (var m in ViewModel.Models)
            {
                m.InitialPosition.x = m.Position.X;
                m.InitialPosition.y = m.Position.Y;
                m.InitialPosition.z = m.Position.Z;
            }

            newModel.InitialPosition.x = newModel.Position.X;
            newModel.InitialPosition.y = newModel.Position.Y;
            newModel.InitialPosition.z = newModel.Position.Z;
              
            ViewModel.Models.Add(newModel);
            ViewModel.SelectedModel = newModel;

            MainWindow.main.threeDControl.InvokeGL(() =>
            {
                newModel.Drawer.Init();
                MainWindow.main.threeDControl.UpdateChanges();
            });
        }

        // =====================================================================
        //  CloneObject
        // =====================================================================
        private bool CloneObject(ThreeDModel model)
        {
            ThreeDModel newModel = new ThreeDModel();
            model.CopyTo(newModel); 
            Autoposition(newModel);
            newModel.UpdateOutOfBound();
            ViewModel.Models.Add(newModel);

            MainWindow.main.threeDControl.InvokeGL(() =>
            {
                newModel.Drawer.Init();
            });
            return true;
        }

        public void CloneObject()
        {
            List<ThreeDModel> cloneModels = GetSelectedPrintModels();
            foreach (var pm in cloneModels) CloneObject(pm);
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
                    dlg.Owner = MainWindow.main;
                dlg.ShowDialog();
                if (dlg.gIsScale) DoAutoScale(model);
                else if (dlg.gIsInch) DoMmToInch(model);
            }
        }

        // =====================================================================
        //  RemoveModel / RemoveAllSelectedModels
        // =====================================================================
        private void RemoveModel(ThreeDModel model)
        {
            // ThreeDModel
            for (int i = 0; i < ViewModel.Models.Count; i++)
                if (ViewModel.Models[i] == model) { ViewModel.Models.RemoveAt(i); break; }

            model.Clear();
        }

        public void buttonRemoveSTL_Click(object sender, EventArgs e) => RemoveModel(ViewModel.SelectedModel);

        private bool IsValidPrintModel(ThreeDModel model)
            => model.Name != "Unknown" &&
               typeof(ThreeDModel) == model.GetType() &&
               model.Model != null;

        // =====================================================================
        //  Autoposition
        // =====================================================================
        bool Autoposition(ThreeDModel newModel)
        {
            List<ThreeDModel> allModels = new List<ThreeDModel>(ViewModel.Models);

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

            var packer    = new RectPacker(1, 1);
            var outPacker = new OutRectPacker(1000);
            int border    = 1;
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
                float xCenter   = (2000 - outPacker.w) / 2f;
                float yCenter   = (2000 - outPacker.h) / 2f;
                float xOrigPos  = xOff + xCenter + outPacker.vRects[0].x + border - 1000;
                float yOrigPos  = yOff + yCenter + outPacker.vRects[0].y + border - 1000;
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

        public void DoMmToInch(ThreeDModel model)
        {
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

        public void DoInchtomm(ThreeDModel model)
        {
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Prevent the window from actually closing
            this.Hide();
        }
    }
}
