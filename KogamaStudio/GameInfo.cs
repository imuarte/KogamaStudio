using Il2Cpp;
using MelonLoader;
using System.Collections;
using System.IO;
using UnityEngine;

namespace KogamaStudio;

// tysm KogamaTools
internal static class GameInfo
{
    internal static int WorldObjectCount;
    internal static int LogicObjectCount;
    internal static int LinkCount;
    internal static int ObjectLinkCount;
    internal static int UniquePrototypeCount;
    internal static int PrototypeCount;
    internal static int Ping;
    internal static float Fps;
    internal static string GameVersion = Application.version.ToString();

    internal static int PlayerCount;
    internal static string LocalPlayerName = "";
    internal static string ServerIP = "";
    internal static int ProfileID;
    internal static int PlanetID = -1;
    internal static string GameMode = "";
    internal static string Region = "";
    internal static string Language = "";

    private static int _lastPlanetID = -1;

    internal static void Init()
    {
        MelonCoroutines.Start(UpdateCoroutine());
    }

    private static IEnumerator UpdateCoroutine()
    {
        var wait = new WaitForSeconds(0.1f);
        while (true)
        {
            yield return wait;
            UpdateMetrics();
        }
    }

    private static void UpdateMetrics()
    {
        WorldObjectCount = MVGameControllerBase.WOCM.worldObjects.Count;
        LogicObjectCount = GetLogicObjectCount();
        LinkCount = MVGameControllerBase.Game.worldNetwork.links.links.Count;
        ObjectLinkCount = MVGameControllerBase.Game.worldNetwork.objectLinks.objectLinks.Count;
        UniquePrototypeCount = MVGameControllerBase.Game.worldNetwork.worldInventory.runtimePrototypes.Count;
        PrototypeCount = GetPrototypeCount();
        Ping = MVGameControllerBase.Game.Peer.RoundTripTime;
        Fps = 1 / Time.smoothDeltaTime;

        var players = MVGameControllerBase.Game?.playerContainer?.players;
        PlayerCount = players?.Count ?? 0;

        var localWoId = MVGameControllerBase.LocalPlayer?.WoId ?? -1;
        if (players != null && localWoId != -1 && players.ContainsKey(localWoId))
            LocalPlayerName = players[localWoId]?.UserProfileData?.UserName ?? "";

        try
        {
            var gsd = MVGameControllerBase.GameSessionData;
            if (gsd != null)
            {
                ServerIP = gsd.serverIP ?? "";
                ProfileID = gsd.profileID;
                PlanetID = gsd.planetID;
                GameMode = gsd.gameMode.ToString();
                Region = gsd.region ?? "";
                Language = gsd.language ?? "";
            }
        }
        catch { }

        if (PlanetID != -1 && PlanetID != _lastPlanetID)
        {
            _lastPlanetID = PlanetID;
            EnsureProjectFolder(PlanetID);
            Explorer.ExplorerManager.RefreshWhenReady();
        }

        SendUpdate();
    }

    private static void SendUpdate()
    {
        var data = $"fps={Fps:F1},ping={Ping}" +
                   $",worldobjects={WorldObjectCount},logicobjects={LogicObjectCount}" +
                   $",links={LinkCount},objectlinks={ObjectLinkCount}" +
                   $",prototypes={PrototypeCount},uniqueprototypes={UniquePrototypeCount}" +
                   $",version={GameVersion}" +
                   $",players={PlayerCount},localplayer={LocalPlayerName}" +
                   $",serverip={ServerIP},profileid={ProfileID},planetid={PlanetID}" +
                   $",gamemode={GameMode},region={Region},language={Language}";
        PipeClient.SendCommand($"gameinfo_update|{data}");
    }

    private static void EnsureProjectFolder(int planetID)
    {
        var path = PathHelper.GetProjectPath(planetID);
        Directory.CreateDirectory(path);
        MelonLogger.Msg($"[GameInfo] Project folder: {path}");
    }

    private static int GetPrototypeCount()
    {
        int count = 0;
        foreach (MVWorldObjectClient wo in MVGameControllerBase.WOCM.worldObjects.Values)
        {
            if (wo.HasInteractionFlag(InteractionFlags.HasCubeModel))
                count++;
        }
        return count;
    }

    private static int GetLogicObjectCount()
    {
        int count = 0;
        foreach (MVWorldObjectClient wo in MVGameControllerBase.WOCM.worldObjects.Values)
        {
            if (wo.HasInputConnector || wo.HasObjectConnector || wo.HasOutputConnector)
                count++;
        }
        return count;
    }
}
