using Il2Cpp;
using MelonLoader;
using Newtonsoft.Json;

namespace KogamaStudio.Explorer;

internal static class ExplorerManager
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KogamaStudio", "explorer_objects.json"
    );

    internal static void Refresh()
    {
        var wocm = MVGameControllerBase.WOCM;
        if (wocm == null) return;

        var ids = new Il2CppSystem.Collections.Generic.HashSet<int>();
        wocm.GetAllWoIds(0, ids);

        var entries = new System.Collections.Generic.List<object>();
        foreach (int id in ids)
        {
            var wo = wocm.GetWorldObjectClient(id);
            if (wo == null) continue;
            entries.Add(new { id, type = wo.type.ToString() });
        }

        try
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(entries));
            PipeClient.SendCommand("explorer_ready");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[ExplorerManager] {ex.Message}");
        }
    }
}
