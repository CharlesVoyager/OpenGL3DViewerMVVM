using OpenGL3DViewerMVVM.Draw;
using OpenTK.Mathematics;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using View3D;
using View3D.model.geom;
using View3D.ModelObjectTool;
using View3D.view;

#nullable disable

namespace OpenGL3DViewerMVVM.ModelLib.model
{
    public class Coord3D
    {
        double x = 0, y = 0, z = 0;
       
        private readonly Action<double, double, double> updateBoundingBoxByShift;
        public Coord3D(Action<double, double, double> operation)
        {
            updateBoundingBoxByShift = operation;
        }

        public double X
        {
            get { return x; }
            set 
            {
                double old = x;
                x = value;
                updateBoundingBoxByShift(x - old, 0, 0);
            }
        }

        public double Y
        {
            get { return y; }
            set
            {
                double old = y;
                y = value;
                updateBoundingBoxByShift(0, y - old, 0);
            }
        }

        public double Z
        {
            get { return z; }
            set
            {
                double old = z;
                z = value;
                updateBoundingBoxByShift(0, 0, z - old);
            }
        }
    }

    public class ThreeDModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool selected = false;

        private Coord3D position = null;   
        private RHVector3 rotation = new RHVector3(0, 0, 0);    
        private RHVector3 scale = new RHVector3(1, 1, 1);


        public RHVector3 InitialPosition = new RHVector3(0, 0, 0);


        public TopoModel Model;         // Original triangles data from 3D Model file (Stl or Glb).

        public Submesh Mesh;            // Centerized triangles data.

        public ModelGLDraw Drawer;

        public RHBoundingBox BoundingBox;
     
        public Matrix4 trans;

        public ThreeDModel()
        {
            position = new Coord3D(updateBoundingBoxByShift);
            Model = new TopoModel();
            Mesh = new Submesh();
            Drawer = new ModelGLDraw(this);
            BoundingBox = new RHBoundingBox();
        }

        bool pointInPrintArea(float x, float y, float z)
        {
            double epsilon = 1e-4; // 0.0001

            if (z < -0.1 || z > SettingsService.Instance.Settings.PrintAreaHeight)
                return false;

            if (x < -epsilon || x > SettingsService.Instance.Settings.PrintAreaWidth + epsilon) return false;
            if (y < -epsilon || y > SettingsService.Instance.Settings.PrintAreaDepth + epsilon) return false;

            return true;
        }

        public void UpdateOutOfBound()
        {
            if (    !pointInPrintArea(xMin, yMin, zMin) ||
                    !pointInPrintArea(xMax, yMin, zMin) ||
                    !pointInPrintArea(xMin, yMax, zMin) ||
                    !pointInPrintArea(xMax, yMax, zMin) ||
                    !pointInPrintArea(xMin, yMin, zMax) ||
                    !pointInPrintArea(xMax, yMin, zMax) ||
                    !pointInPrintArea(xMin, yMax, zMax) ||
                    !pointInPrintArea(xMax, yMax, zMax))
            {
                Outside = true;
            }
            else
            {
                Outside = false;
            }
        }

        public void CopyTo(ThreeDModel stl)
        {
            Model.CopyTo(stl.Model);   // NOTE: Just clone Model is enough. Drawer/BoundingBox do not need to clone.
            Mesh.CopyTo(stl.Mesh);
            stl.Name = Name;
            stl.position.X = position.X;
            stl.position.Y = position.Y;
            stl.position.Z = position.Z;
            stl.Scale.x = Scale.x;
            stl.Scale.y = Scale.y;
            stl.Scale.z = Scale.z;
            stl.Rotation.x = Rotation.x;
            stl.Rotation.y = Rotation.y;
            stl.Rotation.z = Rotation.z;
            stl.trans = trans;
            stl.Selected = false;
            BoundingBox.CopyTo(stl.BoundingBox);    // NOTE: This must be after copying position becuse setting position will update bounding box.
        }

        public void Clear()
        {
            Model.Clear();
            Mesh.Clear();
            BoundingBox.Clear();

            Name = "Unknown";
            Outside = false;

            MainWindow.main.threeDControl.InvokeGL(() =>
            {
                Drawer.Dispose();
            });
        }

        /// <summary>
        /// Translate Object, so that the lowest point is 0.
        /// </summary>
        public void Land() => LandToMinZ(0);

        // Keep same height to the printer base after rotation.
        public void LandToMinZ(float targetMinZ)
        {
            if (Math.Abs(targetMinZ - zMin) < 0.001) return;

            float shiftZ = targetMinZ - zMin;
            Position.Z += shiftZ;

            UpdateTransMatrix();
        }

        // Scale → Rotate → Translate (applied right-to-left in matrix multiplication):
        public void UpdateTransMatrix()
        {
            Matrix4 scale = Matrix4.CreateScale(
                     (float)(Scale.x != 0 ? Scale.x : 1),
                     (float)(Scale.y != 0 ? Scale.y : 1),
                     (float)(Scale.z != 0 ? Scale.z : 1)
            );

            Matrix4 rotX = Matrix4.CreateRotationX((float)(Rotation.x * Math.PI / 180.0));
            Matrix4 rotY = Matrix4.CreateRotationY((float)(Rotation.y * Math.PI / 180.0));
            Matrix4 rotZ = Matrix4.CreateRotationZ((float)(Rotation.z * Math.PI / 180.0));

            Matrix4 transl = Matrix4.CreateTranslation((float)Position.X, (float)Position.Y, (float)Position.Z);

            // Combine: Scale → RotX → RotY → RotZ → Translate
            trans = scale * rotX * rotY * rotZ * transl;
        }

        private unsafe void updateBoundingBox()
        {
            Stopwatch sw = Stopwatch.StartNew();

            BoundingBox.Clear();

            if (Mesh.glVertices.Length == 0)
                return;

            ModelMatrix mtx = ModelObjectToolHelper.ToModelMatrix(trans);
            fixed (float* ptr = &Mesh.glVertices[0])
            {
                BoundingBox3 box3 = ModelObjectToolWrapper.Instance.Tool.GetBoundingBox(mtx, ptr, Mesh.glVertices.Length);
                BoundingBox.Add(box3.MaxX, box3.MaxY, box3.MaxZ);
                BoundingBox.Add(box3.MinX, box3.MinY, box3.MinZ);
            }

            Debug.WriteLine("[ThreeDModel.updateBoundingBox]==> Elapsed Time: " + sw.ElapsedMilliseconds.ToString());
        }

        public void UpdateBoundingBoxAndMatrix()
        {
            //Stopwatch sw = Stopwatch.StartNew();

            UpdateTransMatrix(); // Must update trans Matrix before updating Bounding Box.

            updateBoundingBox();

            //Debug.WriteLine("[ThreeDModel.UpdateBoundingBoxAndMatrix]==> Elapsed Time: " + sw.ElapsedMilliseconds.ToString());
        }

        // This function is used when moving the object for saving bounding box compuation.
        // NOTE NOTE NOTE: If the model is rotated, the bounding box can not be obtained just through trans matrix but compute all vertices in regular way.
        // Important Test Case: Rotate the model 40 degress -> Move the object to check if the bounding box is align correctly.
        void updateBoundingBoxByShift(double shiftX, double shiftY, double shiftZ)
        {
            BoundingBox.MaxPoint.x += shiftX;
            BoundingBox.MinPoint.x += shiftX;

            BoundingBox.MaxPoint.y += shiftY;
            BoundingBox.MinPoint.y += shiftY;

            BoundingBox.MaxPoint.z += shiftZ;
            BoundingBox.MinPoint.z += shiftZ;
        }

        public void ModelToMesh()
        {
            //Stopwatch sw = Stopwatch.StartNew();

            Mesh.Clear();

            Mesh.EnsureCapacity(Model.drawTriangles.Count, Model.HasColor());

            var ranges = Model.primitiveMaterials
                .OrderBy(r => r.StartTriangle)
                .ToList();
            int rangeIndex = 0;
            int nextRangeEnd = ranges.Count > 0 ? ranges[0].StartTriangle + ranges[0].TriangleCount : 0;

            // Fill Mesh with checking RAM 
            int cnt = 0;
            foreach (TopoTriangle t in Model.drawTriangles)
            {
                if (0 == cnt % 50000)
                {
                    if (!Utils.RamTools.IsRamSizeValid())
                    {
                        throw new System.OutOfMemoryException();
                    }
                }

                while (rangeIndex < ranges.Count && cnt >= nextRangeEnd)
                {
                    rangeIndex++;
                    nextRangeEnd = rangeIndex < ranges.Count
                        ? ranges[rangeIndex].StartTriangle + ranges[rangeIndex].TriangleCount
                        : 0;
                }

                int materialIndex = rangeIndex < ranges.Count &&
                                    cnt >= ranges[rangeIndex].StartTriangle &&
                                    cnt < nextRangeEnd
                    ? ranges[rangeIndex].MaterialIndex
                    : -1;

                Mesh.DrawRanges.Add(new Submesh.DrawRange
                {
                    StartVertex = cnt * 3,
                    VertexCount = 3,
                    MaterialIndex = materialIndex
                });

                Mesh.AddTriangle(
                    t.Vertices[0].pos.Subtract(Model.boundingBox.Center),
                    t.Vertices[1].pos.Subtract(Model.boundingBox.Center),
                    t.Vertices[2].pos.Subtract(Model.boundingBox.Center),
                    t.VertexNormals[0],
                    t.VertexNormals[1],
                    t.VertexNormals[2],
                    t.Color);
                cnt++;
            }
            // <>

            Mesh.selected = Selected;

            //Debug.WriteLine("[PrintModel.Paint]==> Elapsed Time: " + sw.ElapsedMilliseconds.ToString());
        }

        public void CopyTopoModelBoundingBoxToPrintModel()
        {
            BoundingBox.Add(Model.boundingBox); // Copy TopoModel's Bounding Box to PrintModel.
        }

        public void ObjectMoved(float dx, float dy)
        {
            float maxX = SettingsService.Instance.Settings.PrintAreaWidth * 1.2f;
            float minX = -SettingsService.Instance.Settings.PrintAreaWidth * 0.2f;
            float maxY = SettingsService.Instance.Settings.PrintAreaDepth * 1.2f;
            float minY = -SettingsService.Instance.Settings.PrintAreaDepth * 0.2f;

            if (dx < 0 && Position.X + dx > minX)  // If the boject is out of bound, allow to move it back to the bound area.
                PositionX += dx;
            else if (Position.X + dx < maxX && Position.X + dx > minX)
                PositionX += dx;

            if (dy < 0 && Position.Y + dy > minY)
                PositionY += dy;
            else if (Position.Y + dy < maxY && Position.Y + dy > minY)
                PositionY += dy;
        }

        public string Name { get; set; } = "Unknown";

        public int Triangles
        {
            get { return Model.drawTriangles.Count; }
        }

        bool outside = false;
        public bool Outside 
        {
            get { return outside; } 
            set
            {
                outside = value;
                OnPropertyChanged();    // Update DataGrid in STLComposer.
                MainWindow.main.viewModel.UpdateOutOfBound();
            }
        }

        public float xMin
        {
            get { return (float)BoundingBox.MinPoint.x; }
        }

        public float yMin
        {
            get { return (float)BoundingBox.MinPoint.y; }
        }

        public float zMin
        {
            get { return (float)BoundingBox.MinPoint.z; }
        }

        public float xMax
        {
            get { return (float)BoundingBox.MaxPoint.x; }
        }

        public float yMax
        {
            get { return (float)BoundingBox.MaxPoint.y; }
        }

        public float zMax
        {
            get { return (float)BoundingBox.MaxPoint.z; }
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; }
        }

        public Coord3D Position
        {
            get { return position; }
        }

        public RHVector3 Rotation
        {
            get { return rotation; }
        }

        public RHVector3 Scale
        {
            get { return scale; }
        }

        void updateChange(string propertyName = null)
        {
            OnPropertyChanged(propertyName);    // Notify UI that property has changed.
       
            if (propertyName.Contains("Position"))
                UpdateTransMatrix();    // Bounding box will be automatically updated by position change.
            else
                UpdateBoundingBoxAndMatrix();

            UpdateOutOfBound();
            MainWindow.main.threeDControl.UpdateChanges();
        }

        public double PositionX
        {
            get { return position.X; }
            set 
            { 
                position.X = value;
                updateChange(nameof(PositionX));
            }
        }

        public double PositionY
        {
            get { return position.Y; }
            set 
            { 
                position.Y = value;
                updateChange(nameof(PositionY));
            }
        }

        public double PositionZ
        {
            get { return position.Z; }
            set 
            { 
                position.Z = value;
                updateChange(nameof(PositionZ));
            }
        }

        public double RotationX
        {
            get { return rotation.x; }
            set 
            { 
                rotation.x = value;
                updateChange(nameof(RotationX));
            }
        }

        public double RotationY
        {
            get { return rotation.y; }
            set 
            { 
                rotation.y = value;
                updateChange(nameof(RotationY));
            }
        }

        public double RotationZ
        {
            get { return rotation.z; }
            set 
            { 
                rotation.z = value;
                updateChange(nameof(RotationZ));
            }
        }

        public double ScaleX
        {
            get { return scale.x; }
            set 
            { 
                scale.x = value;
                updateChange(nameof(ScaleX));
            }
        }

        public double ScaleY
        {
            get { return scale.y; }
            set 
            { 
                scale.y = value;
                updateChange(nameof(ScaleY));
            }
        }

        public double ScaleZ
        {
            get { return scale.z; }
            set 
            { 
                scale.z = value;
                updateChange(nameof(ScaleZ));
            }
        }

        public string OriginalModelSize
        {
            get { return Model.boundingBox.Size.ToString(); }
        }
    }
}
