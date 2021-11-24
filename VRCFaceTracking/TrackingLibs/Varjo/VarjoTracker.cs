using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VRCFaceTracking.Varjo
{
    // a dumb memory structure to copy all the data between the mod and the companion
    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryEye
    {
        public bool opened;
        public double pupilSize;
        public double x;
        public double y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryData
    {
        public bool shutdown;
        public bool calibrated;
        public MemoryEye leftEye;
        public MemoryEye rightEye;
        public MemoryEye combined;
    }

    public class VarjoTracker
    {
        
        private MemoryMappedFile MemMapFile;
        private MemoryMappedViewAccessor ViewAccessor;
        public MemoryData memoryGazeData;
        private Process CompanionProcess;

        public bool ConnectToPipe()
        {
            if(!varjo_IsAvailable())
            {
                MelonLogger.Msg("Varjo headset isn't detected");
                return false;
            }
            var modDir = GetModTempPath();

            CompanionProcess = new Process();
            CompanionProcess.StartInfo.WorkingDirectory = modDir;
            CompanionProcess.StartInfo.FileName = Path.Combine(modDir, "VarjoCompanion.exe");
            CompanionProcess.Start();

            for (int i = 0; i < 5;i++)
            {
                try
                {
                    MemMapFile = MemoryMappedFile.OpenExisting("VarjoEyeTracking");
                    ViewAccessor = MemMapFile.CreateViewAccessor();
                    return true;
                }
                catch (FileNotFoundException)
                {
                    MelonLogger.Warning("VarjoEyeTracking mapped file doesn't exist; the companion app probably isn't running");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Could not open the mapped file: " + ex);
                    return false;
                }
                Thread.Sleep(500);
            }
            
            return false;
        }

        public void Update()
        {
            if (MemMapFile == null) return;
            ViewAccessor.Read(0, out memoryGazeData);
        }

        public void Teardown()
        {
            if (MemMapFile == null) return;
            memoryGazeData.shutdown = true; // tell the companion app to shut down gracefully
            ViewAccessor.Write(0, ref memoryGazeData);
            MemMapFile.Dispose();
            CompanionProcess.Close();
        }

        private string GetModTempPath()
        {
            var melonInfo = Assembly.GetExecutingAssembly().CustomAttributes.ToList()
                .Find(a => a.AttributeType == typeof(MelonInfoAttribute));
            var dirName = Path.Combine(Path.GetTempPath(), melonInfo.ConstructorArguments[1].Value.ToString());
            return dirName;
        }

        // Sadly it's impossible to do more stuff through the application with the same PID as the OpenVR/VRChat one
        [DllImport("VarjoLib", CharSet = CharSet.Auto)]
        public static extern bool varjo_IsAvailable();
    }
}
