using OpenGL3DViewerMVVM.model.geom;
using OpenGL3DViewerMVVM.View;
using System.Drawing;
using System.IO;
using System.Text;

namespace OpenGL3DViewerMVVM.ModelLib.Utils
{
    class DebugTools
    {
#if false
        void DebugLog()
        {
            DebugTools dbgTools = new DebugTools();
            int sn = 0;

            foreach (var model in viewModel.Models)
            {
                if (model.Model.materials.Count > 0)
                {
                    foreach (var mat in model.Model.materials)
                    {
                        dbgTools.DumpMaterials(mat, AppDomain.CurrentDomain.BaseDirectory + "DebugOutput" + sn.ToString());
                        sn++;
                    }
                }
            }
        }
#endif
        public void DumpMaterials(PbrMaterial pm, string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            LogTextFields(pm, outputDir);
            LogBitmapField(nameof(pm.BaseColorTexture), pm.BaseColorTexture, outputDir);
            LogBitmapField(nameof(pm.MetallicRoughnessTexture), pm.MetallicRoughnessTexture, outputDir);
            LogBitmapField(nameof(pm.NormalTexture), pm.NormalTexture, outputDir);
            LogBitmapField(nameof(pm.OcclusionTexture), pm.OcclusionTexture, outputDir);
            LogBitmapField(nameof(pm.EmissiveTexture), pm.EmissiveTexture, outputDir);
        }

        private void LogTextFields(PbrMaterial pm, string outputDir)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== PbrMaterial ===");
            sb.AppendLine();

            // Texture presence flags
            sb.AppendLine("-- Textures --");
            sb.AppendLine($"BaseColorTexture:             {(pm.BaseColorTexture != null ? "present" : "null")}");
            sb.AppendLine($"MetallicRoughnessTexture:     {(pm.MetallicRoughnessTexture != null ? "present" : "null")}");
            sb.AppendLine($"NormalTexture:                {(pm.NormalTexture != null ? "present" : "null")}");
            sb.AppendLine($"OcclusionTexture:             {(pm.OcclusionTexture != null ? "present" : "null")}");
            sb.AppendLine($"EmissiveTexture:              {(pm.EmissiveTexture != null ? "present" : "null")}");
            sb.AppendLine();

            // Float factors
            sb.AppendLine("-- Factors --");
            sb.AppendLine($"BaseColorFactor (RGBA):       {FormatFloatArray(pm.BaseColorFactor)}");
            sb.AppendLine($"MetallicFactor:               {pm.MetallicFactor:F6}");
            sb.AppendLine($"RoughnessFactor:              {pm.RoughnessFactor:F6}");
            sb.AppendLine($"EmissiveFactor (RGB):         {FormatFloatArray(pm.EmissiveFactor)}");

            string textPath = Path.Combine(outputDir, "material.txt");
            File.WriteAllText(textPath, sb.ToString());
            Console.WriteLine($"[PbrMaterial] Text log  → {textPath}");
        }

        private void LogBitmapField(string fieldName, Bitmap? bitmap, string outputDir)
        {
            if (bitmap == null)
            {
                Console.WriteLine($"[PbrMaterial] {fieldName}: null, skipped");
                return;
            }

            string bmpPath = Path.Combine(outputDir, $"{fieldName}.bmp");

            // Clone into a clean Bitmap to avoid GDI+ stream-lock issues
            using var clean = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(clean))
                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);

            clean.Save(bmpPath, System.Drawing.Imaging.ImageFormat.Bmp);
            Console.WriteLine($"[PbrMaterial] Bitmap log → {bmpPath}  ({bitmap.Width}×{bitmap.Height})");
        }

        private string FormatFloatArray(float[] values)
            => "[ " + string.Join(", ", Array.ConvertAll(values, v => v.ToString("F6"))) + " ]";
    }
}
