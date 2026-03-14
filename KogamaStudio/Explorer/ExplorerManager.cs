using Il2Cpp;
using MelonLoader;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KogamaStudio.Explorer;

internal static class ExplorerManager
{
    internal static string GetObjectsFilePath(int planetID) =>
        Path.Combine(PathHelper.GetProjectPath(planetID), "objects.json");

    internal static string GetMetaFilePath(int planetID) =>
        Path.Combine(PathHelper.GetProjectPath(planetID), "object_meta.json");

    internal static void RefreshWhenReady()
    {
        MelonCoroutines.Start(RefreshCoroutine());
    }

    private static IEnumerator RefreshCoroutine()
    {
        var wait = new WaitForSeconds(0.5f);
        while (MVGameControllerBase.WOCM?.worldObjects == null ||
               MVGameControllerBase.WOCM.worldObjects.Count == 0)
        {
            yield return wait;
        }
        Refresh();
    }

    internal static void Refresh()
    {
        var wocm = MVGameControllerBase.WOCM;
        if (wocm == null) return;

        int planetID = GameInfo.PlanetID;
        if (planetID == -1)
        {
            MelonLogger.Warning("[ExplorerManager] PlanetID not set, skipping refresh");
            return;
        }

        // Load existing meta (sparse - only objects with assigned names)
        var metaPath = GetMetaFilePath(planetID);
        var meta = new Dictionary<int, ObjectMetaEntry>();
        if (File.Exists(metaPath))
        {
            try { meta = JsonConvert.DeserializeObject<Dictionary<int, ObjectMetaEntry>>(File.ReadAllText(metaPath)) ?? meta; }
            catch { }
        }

        var typeCounts = new Dictionary<string, int>();
        var entries = new List<object>();
        foreach (var kvp in wocm.worldObjects)
        {
            var wo = kvp.Value;
            if (wo == null) continue;

            var type = wo.type.ToString();
            meta.TryGetValue(kvp.Key, out var m);

            bool hasMeta = m != null && !string.IsNullOrEmpty(m.Name);
            string name;
            if (hasMeta)
            {
                name = m.Name;
            }
            else
            {
                int count = typeCounts.GetValueOrDefault(type, 0);
                typeCounts[type] = count + 1;
                name = count == 0 ? type : $"{type} {count}";
            }

            entries.Add(new { id = kvp.Key, type, name, itemId = hasMeta ? -1 : (int)wo.ItemId });
        }

        try
        {
            var filePath = GetObjectsFilePath(planetID);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(entries));
            PipeClient.SendCommand($"explorer_ready|{filePath}");
            MelonLogger.Msg($"[ExplorerManager] Saved {entries.Count} objects to {filePath}");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[ExplorerManager] {ex.Message}");
        }
    }
}
