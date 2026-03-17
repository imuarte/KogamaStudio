
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KogamaStudio.Explorer;

internal class ObjectEntry
{
    [JsonProperty("id")]      public int    id;
    [JsonProperty("order")]   public int    order;
    [JsonProperty("name")]    public string name    = "";
    [JsonProperty("type")]    public string type    = "";
    [JsonProperty("itemId")]  public int    itemId  = -1;
    [JsonProperty("groupId")] public int    groupId = -1;
    [JsonProperty("x")]       public float  x;
    [JsonProperty("y")]       public float  y;
    [JsonProperty("z")]       public float  z;
}

internal static class ExplorerManager
{
    private static bool _loadComplete = false;

    private static string FilePath(int planetID) =>
        Path.Combine(PathHelper.GetProjectPath(planetID), "objects.json");

    private static List<ObjectEntry> Read(int planetID)
    {
        var path = FilePath(planetID);
        if (!File.Exists(path)) return new();
        try   { return JsonConvert.DeserializeObject<List<ObjectEntry>>(File.ReadAllText(path)) ?? new(); }
        catch { return new(); }
    }

    private static int RootGroupId => MVGameControllerBase.WOCM?.RootGroup?.Id ?? -1;

    private static void Write(int planetID, List<ObjectEntry> list)
    {
        try
        {
            File.WriteAllText(FilePath(planetID), JsonConvert.SerializeObject(list));
            PipeClient.SendCommand($"explorer_ready|{FilePath(planetID)}|{RootGroupId}");
        }
        catch (Exception ex) { KogamaStudio.Log.LogError($"[Explorer] Write: {ex.Message}"); }
    }

    internal static void SetOrder(string param)
    {
        int planetID = GameInfo.PlanetID;
        if (planetID == -1) return;

        var ids    = param.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => int.Parse(s.Trim())).ToList();
        var list   = Read(planetID);
        var byId   = list.ToDictionary(e => e.id);
        var result = ids.Where(id => byId.ContainsKey(id)).Select(id => byId[id]).ToList();

        var inOrder = new HashSet<int>(ids);
        foreach (var e in list) if (!inOrder.Contains(e.id)) result.Add(e);
        for (int i = 0; i < result.Count; i++) result[i].order = i;

        Write(planetID, result);
    }

    internal static void Rename(int id, string newName)
    {
        int planetID = GameInfo.PlanetID;
        if (planetID == -1) return;

        var list  = Read(planetID);
        var entry = list.FirstOrDefault(e => e.id == id);
        if (entry == null) return;

        if (string.IsNullOrEmpty(newName))
        {
            entry.name   = entry.type;
            var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);
            if (wo != null) entry.itemId = (int)wo.ItemId;
        }
        else
        {
            entry.name   = newName;
            entry.itemId = -1;
        }
        Write(planetID, list);
    }

    internal static void RefreshWhenReady()
    {
        _loadComplete = false;
        KogamaStudioBehaviour.StartCo(RefreshCoroutine());
    }

    private static IEnumerator RefreshCoroutine()
    {
        var wait = new WaitForSeconds(0.5f);

        while (GameInfo.PlanetID == -1)
            yield return wait;

        int planetID = GameInfo.PlanetID;

        if (File.Exists(FilePath(planetID)))
            PipeClient.SendCommand($"explorer_ready|{FilePath(planetID)}|{RootGroupId}");

        while (MVGameControllerBase.WOCM?.worldObjects == null ||
               MVGameControllerBase.WOCM.worldObjects.Count == 0)
            yield return wait;

        int   lastCount = -1;
        float stableFor = 0f;
        while (stableFor < 2f)
        {
            yield return wait;
            int count = MVGameControllerBase.WOCM?.worldObjects?.Count ?? 0;
            if (count != lastCount) { lastCount = count; stableFor = 0f; }
            else                      stableFor += 0.5f;
        }

        _loadComplete = true;
        Refresh();
    }

    internal static void Refresh()
    {
        var wocm = MVGameControllerBase.WOCM;
        if (wocm == null) return;
        int planetID = GameInfo.PlanetID;
        if (planetID == -1) return;

        var list = Read(planetID);
        list.Sort((a, b) => { int c = a.order.CompareTo(b.order); return c != 0 ? c : a.id.CompareTo(b.id); });

        var liveIds = new HashSet<int>();
        foreach (var kvp in wocm.worldObjects)
            if (kvp.Value != null) liveIds.Add(kvp.Key);

        if (_loadComplete)
            list.RemoveAll(e => !liveIds.Contains(e.id));

        for (int i = 0; i < list.Count; i++) list[i].order = i;

        foreach (var e in list)
        {
            var wo = wocm.GetWorldObjectClient(e.id);
            if (wo == null) continue;
            e.groupId = wo.GroupId;
            var pos = wo.transform?.position ?? UnityEngine.Vector3.zero;
            e.x = pos.x; e.y = pos.y; e.z = pos.z;
        }

        var savedIds   = new HashSet<int>(list.Select(e => e.id));
        var typeCounts = new Dictionary<string, int>();
        foreach (var e in list) { typeCounts.TryGetValue(e.type, out int c); typeCounts[e.type] = c + 1; }

        int nextOrder  = list.Count;
        var newEntries = new List<ObjectEntry>();
        foreach (var kvp in wocm.worldObjects)
        {
            if (kvp.Value == null || savedIds.Contains(kvp.Key)) continue;
            var wo   = kvp.Value;
            var type = wo.type.ToString();
            typeCounts.TryGetValue(type, out int count);
            typeCounts[type] = count + 1;
            var pos = wo.transform?.position ?? UnityEngine.Vector3.zero;
            newEntries.Add(new ObjectEntry
            {
                id      = kvp.Key,
                type    = type,
                name    = count == 0 ? type : $"{type} {count}",
                itemId  = (int)wo.ItemId,
                groupId = wo.GroupId,
                x = pos.x, y = pos.y, z = pos.z
            });
        }
        newEntries.Sort((a, b) => a.id.CompareTo(b.id));
        foreach (var e in newEntries) e.order = nextOrder++;
        list.AddRange(newEntries);

        Write(planetID, list);
    }
}
