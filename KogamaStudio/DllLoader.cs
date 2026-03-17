
using System.Runtime.InteropServices;
using System.IO;

namespace KogamaStudio
{
    internal class DllLoader
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        public static void Load(string dllName)
        {
            string currentDir = Path.GetDirectoryName(typeof(DllLoader).Assembly.Location);
            string dllPath = Path.Combine(currentDir, dllName);

            IntPtr handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                KogamaStudio.Log.LogError($"Failed to load {dllPath}. Error code: {errorCode}");
            }
            else
            {
                KogamaStudio.Log.LogInfo($"Loaded: {dllPath}");
            }
        }
    }
}
