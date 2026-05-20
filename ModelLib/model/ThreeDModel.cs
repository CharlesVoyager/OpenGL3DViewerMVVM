using OpenGL3DViewerMVVM.Draw;
using OpenGL3DViewerMVVM.model.geom;
using OpenGL3DViewerMVVM.ModelObjectTool;
using OpenGL3DViewerMVVM.View;
using OpenTK.Mathematics;
using System.Diagnostics;

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

        public override string ToString()
        {
            return "(" + x.ToString("0.000") + ", " + y.ToString("0.000") + ", " + z.ToString("0.000") + ")";
        }
    }

    public class ThreeDModel : ViewModelBase
    {   
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

        public void UpdateOutside()
        {
            double epsilon = 1e-4; // 0.0001
            if ( xMin > -epsilon && xMax < SettingsService.Instance.Settings.PrintAreaWidth + epsilon &&
                 yMin > -epsilon && yMax < SettingsService.Instance.Settings.PrintAreaDepth + epsilon &&
                 zMin > -epsilon && zMax < SettingsService.Instance.Settings.PrintAreaHeight + epsilon)
            {
                Outside = false;
            }
            else
            {
                Outside = true;
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
            stl.scale.x = scale.x;
            stl.scale.y = scale.y;
            stl.scale.z = scale.z;
            stl.rotation.x = rotation.x;
            stl.rotation.y = rotation.y;
            stl.rotation.z = rotation.z;
            stl.trans = trans;
            stl.Selected = false;
            BoundingBox.CopyTo(stl.BoundingBox);    // NOTE: This must be after copying position becuse setting position will update bounding box.
        }

        public void Dispose()
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

        public void Reset()
        {
            PositionX = InitialPosition.x;
            PositionY = InitialPosition.y;
            PositionZ = InitialPosition.z;
            RotationX = 0;
            RotationY = 0;
            RotationZ = 0;
            ScaleX = 1;
            ScaleY = 1;
            ScaleZ = 1;

            UpdateBoundingBoxAndMatrix();
            Land();
        }


        /// <summary>
        /// Translate Object, so that the lowest point is 0.
        /// </summary>
        public void Land() => landToMinZ(0);

        // Keep same height to the printer base after rotation.
        void landToMinZ(double targetMinZ)
        {
            if (Math.Abs(targetMinZ - zMin) < 0.001) return;

            double shiftZ = targetMinZ - zMin;
            PositionZ += shiftZ;
        }

        // Scale → Rotate → Translate (applied right-to-left in matrix multiplication):
        public void UpdateTransMatrix()
        {
            Matrix4 scaleMatrix = Matrix4.CreateScale(
                     (float)(scale.x != 0 ? scale.x : 1),
                     (float)(scale.y != 0 ? scale.y : 1),
                     (float)(scale.z != 0 ? scale.z : 1)
            );

            Matrix4 rotX = Matrix4.CreateRotationX((float)(rotation.x * Math.PI / 180.0));
            Matrix4 rotY = Matrix4.CreateRotationY((float)(rotation.y * Math.PI / 180.0));
            Matrix4 rotZ = Matrix4.CreateRotationZ((float)(rotation.z * Math.PI / 180.0));

            Matrix4 transl = Matrix4.CreateTranslation((float)position.X, (float)position.Y, (float)position.Z);

            // Combine: Scale → RotX → RotY → RotZ → Translate
            trans = scaleMatrix * rotX * rotY * rotZ * transl;
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
            Matrix4 previousModelMatrix = trans;

            UpdateTransMatrix(); // Must update trans Matrix before updating Bounding Box.

            if (trans != previousModelMatrix)   // Compute bounding box only when the model matrix has changed. This can save a lot of time.
                updateBoundingBox();
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

                if (materialIndex != -1)
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

            if (dx < 0 && PositionX + dx > minX)  // If the boject is out of bound, allow to move it back to the bound area.
                PositionX += dx;
            else if (PositionX + dx < maxX && PositionX + dx > minX)
                PositionX += dx;

            if (dy < 0 && PositionY + dy > minY)
                PositionY += dy;
            else if (PositionY + dy < maxY && PositionY + dy > minY)
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

        public double PositionX
        {
            get { return position.X; }
            set 
            { 
                position.X = value;

                UpdateTransMatrix();   // Bounding box will be automatically updated by position change.
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(PositionX));
            }
        }

        public double PositionY
        {
            get { return position.Y; }
            set 
            { 
                position.Y = value;

                UpdateTransMatrix();   // Bounding box will be automatically updated by position change.
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(PositionY));
            }
        }

        public double PositionZ
        {
            get { return position.Z; }
            set 
            { 
                position.Z = value;

                UpdateTransMatrix();   // Bounding box will be automatically updated by position change.
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(PositionZ));
            }
        }

        public double RotationX
        {
            get { return rotation.x; }
            set 
            {
                if (value > 360)
                    rotation.x = 360;
                else if (value < 0)
                    rotation.x = 0;
                else 
                    rotation.x = value;

                UpdateBoundingBoxAndMatrix();
                Land();
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(RotationX));
            }
        }

        public double RotationY
        {
            get { return rotation.y; }
            set
            {
                if (value > 360)
                    rotation.y = 360;
                else if (value < 0)
                    rotation.y = 0;
                else
                    rotation.y = value;

                UpdateBoundingBoxAndMatrix();
                Land();
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(RotationY));
            }
        }

        public double RotationZ
        {
            get { return rotation.z; }
            set
            {
                if (value > 360)
                    rotation.z = 360;
                else if (value < 0)
                    rotation.z = 0;
                else
                    rotation.z = value;

                UpdateBoundingBoxAndMatrix();
                Land();
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(RotationZ));
            }
        }

        public double ScaleX
        {
            get { return scale.x; }
            set
            {
                if (value < 0)
                    scale.x = 0;
                else
                    scale.x = value;

                UpdateBoundingBoxAndMatrix();
                Land();
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(ScaleX));
            }
        }

        public double ScaleY
        {
            get { return scale.y; }
            set
            {
                if (value < 0)
                    scale.y = 0;
                else
                    scale.y = value;

                UpdateBoundingBoxAndMatrix();
                Land();
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(ScaleY));
            }
        }

        public double ScaleZ
        {
            get { return scale.z; }
            set 
            { 
                if (value < 0)
                    scale.z = 0;
                else
                    scale.z = value;

                UpdateBoundingBoxAndMatrix();
                Land();
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(ScaleZ));
            }
        }

        double uniformScaleValue = 1;
        public double UniformScale
        {
            get { return uniformScaleValue; }
            set
            {
                if (value < 0)
                    uniformScaleValue = 0;
                else
                {
                    scale.x = value;
                    scale.y = value;
                    scale.z = value;
                    uniformScaleValue = value;
                }

                UpdateBoundingBoxAndMatrix();
                Land();
                UpdateOutside();
                MainWindow.main.threeDControl.UpdateChanges();

                OnPropertyChanged(nameof(ScaleX));
                OnPropertyChanged(nameof(ScaleY));
                OnPropertyChanged(nameof(ScaleZ));
                OnPropertyChanged(nameof(UniformScale));
            }
        }

        public string OriginalModelSize
        {
            get { return Model.boundingBox.Size.ToString(); }
        }
    }
}
