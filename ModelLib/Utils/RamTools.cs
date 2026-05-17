using System;
using System.Diagnostics;

namespace OpenGL3DViewerMVVM.ModelLib.Utils
{
    public static class RamTools
    {
        const uint UsedLimit_64bit = 5120;     // 5120MB => 5GB
        const uint UsedLimit_32bit = 1536;     // 1536MB => 1.5GB
        const uint RemainMin = 100;            // 100MB
        const ulong LimitPercent = 30;

        public static bool IsRamSizeValid()
        {
            bool valid = true;

#if NET10_0_OR_GREATER
            var gcInfo = GC.GetGCMemoryInfo();
            ulong totalRam = (ulong)gcInfo.TotalAvailableMemoryBytes / 1024 / 1024;  // MB
            ulong committedRam = (ulong)gcInfo.TotalCommittedBytes / 1024 / 1024;  // MB
            ulong availRam = totalRam - committedRam;
#else   // .NET Framework 4.8
            ulong availRam = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1024 / 1024;    // Unit: MB
#endif

            if (availRam < RemainMin || getCurMemoryUsed() >= UsedLimit_64bit)
            {
                valid = false;
            }
            return valid;
        }

        public static uint getCurMemoryUsed()
        {
            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            uint totalMBOfMemoryUsed = Convert.ToUInt16(currentProcess.WorkingSet64 / 1024 / 1024);
            return totalMBOfMemoryUsed;
        }
    }
}
