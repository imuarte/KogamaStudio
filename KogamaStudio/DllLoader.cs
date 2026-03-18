
using System.Runtime.InteropServices;
using System.IO;

namespace KogamaStudio
{
    internal class DllLoader
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        public static bool ImGuiLoaded { get; private set; } = false;
        public static string LoadError { get; private set; } = null;

        public static void Load(string dllName)
        {
            string currentDir = Path.GetDirectoryName(typeof(DllLoader).Assembly.Location);
            string dllPath = Path.Combine(currentDir, dllName);

            KogamaStudio.Log.LogInfo($"Attempting to load: {dllPath}");

            if (!File.Exists(dllPath))
            {
                LoadError = $"DLL not found: {dllPath}";
                KogamaStudio.Log.LogError(LoadError);
                KogamaStudio.Log.LogError("Make sure KogamaStudio-ImGui-Hook.dll is in the BepInEx/plugins folder.");
                return;
            }

            var fileInfo = new FileInfo(dllPath);
            KogamaStudio.Log.LogInfo($"DLL file size: {fileInfo.Length} bytes, last modified: {fileInfo.LastWriteTime}");

            IntPtr handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMsg = errorCode switch
                {
                    126 => "Module not found - missing dependency (vcruntime, d3d11.dll etc.)",
                    193 => "Wrong architecture (x86 vs x64)",
                    5 => "Access denied - antivirus may be blocking the DLL",
                    1114 => "DLL initialization failed - DllMain returned FALSE",
                    225 => "Antivirus blocked the operation",
                    _ => $"Win32 error {errorCode}"
                };
                LoadError = $"Failed to load ImGui hook: {errorMsg}";
                KogamaStudio.Log.LogError($"Failed to load {dllPath}. Error {errorCode}: {errorMsg}");
                KogamaStudio.Log.LogError("If antivirus is blocking, add an exclusion for the BepInEx/plugins folder.");
            }
            else
            {
                ImGuiLoaded = true;
                KogamaStudio.Log.LogInfo($"Successfully loaded: {dllPath} (handle: 0x{handle:X})");
            }
        }
    }
}
