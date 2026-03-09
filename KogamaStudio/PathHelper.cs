
namespace KogamaStudio
{
    public static class PathHelper
    {
        public static string GetPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KogamaStudio"       
            );
        }

        public static string GetMaterialsPath()
        {
            return Path.Combine(
                GetPath(),
                "ResourcePacks",
                "Default",
                "materials"
            );
        }

        public static string GetProjectPath(int planetID)
        {
            return Path.Combine(GetPath(), "Projects", planetID.ToString());
        }
    }
}
