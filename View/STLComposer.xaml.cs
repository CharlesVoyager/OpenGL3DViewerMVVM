using OpenGL3DViewerMVVM.ModelLib.model;
using OpenGL3DViewerMVVM.ModelLib.Utils;
using System.Text;
using System.Windows;
using View3D.model.geom;

#nullable disable

namespace View3D.view
{
    public partial class STLComposer : Window
    {
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

            DataContext = MainWindow.main.viewModel;
        }

        public void translate() { }

        public List<ThreeDModel> GetAllPrintModels()
        {
            var list = new List<ThreeDModel>();
            foreach (var m in MainWindow.main.viewModel.Models)
                if (IsValidPrintModel(m)) list.Add(m);
            return list;
        }

        public List<ThreeDModel> GetSelectedPrintModels()
        {
            var list = new List<ThreeDModel>();
            foreach (var m in MainWindow.main.viewModel.Models)
                if (IsValidPrintModel(m) && m.Selected) list.Add(m);
            return list;
        }

        bool isTooSmall(RHBoundingBox boundingBox)
        {
            // Don't use z size here because some STL files may have very small z size but large x/y size, and they should not be considered as "too small".
            return (boundingBox.Size.x < 10 && boundingBox.Size.y < 10 && boundingBox.Size.z < 10); 
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
            MainWindow.main.viewModel.Models.Add(newModel);

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
            model.Clear();
            MainWindow.main.viewModel.Models.Remove(model);
        }

        public void buttonRemoveSTL_Click(object sender, EventArgs e) => RemoveModel(MainWindow.main.viewModel.SelectedModel);

        private bool IsValidPrintModel(ThreeDModel model)
            => model.Name != "Unknown" &&
               typeof(ThreeDModel) == model.GetType() &&
               model.Model != null;

        // =====================================================================
        //  Autoposition
        // =====================================================================
        public bool Autoposition(ThreeDModel newModel)
        {
            List<ThreeDModel> allModels = new List<ThreeDModel>(MainWindow.main.viewModel.Models);

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
