using HarmonyLib;
using Il2Cpp;
using Il2CppExitGames.Client.Photon;
using Il2CppInterop.Runtime;
using Il2CppMV.Common;
using Il2CppMV.WorldObject;
using Il2CppSystem.Security.Cryptography;
using MelonLoader;
using System.Xml.Linq;
using UnityEngine;
using static Il2Cpp.Interop;
using static Il2CppTMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace KogamaStudio;

[HarmonyPatch]
internal class RecoveryMode
{
    internal static bool RecoveryModeEnabled = false;
    internal static int TargetItemId = -1;
    internal static LoadingScreenHandler LoadingScreenHandlerInstance;

    internal static void EnableRecoveryMode()
    {
        if (RecoveryModeEnabled) return;
        MelonLogger.Msg("Recovery Mode Enabled!");
        PipeClient.SendCommand("game_initialized");
        PipeClient.SendCommand("key_down|F2");
        PipeClient.SendCommand("recovery_mode");
        RecoveryModeEnabled = true;
    }

    internal static void DisableLoadingScreen()
    {
        var loadingScreenHandlers = UnityEngine.Object.FindObjectsOfType<LoadingScreenHandler>();
        foreach (var loadingScreenHandler in loadingScreenHandlers)
        {
            loadingScreenHandler.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(LoadingScreenHandler), "Start")]
    [HarmonyPrefix]
    internal static void LoadingScreenStartPrefix(LoadingScreenHandler __instance)
    {
        LoadingScreenHandlerInstance = __instance;
    }

    internal static bool CorruptionDetected = false;
    private static readonly List<(string desc, string objectId, string actionType)> _pendingProblems = new();
    private static bool _initialized = false;

    private static void NotifyCorruption(string description, string objectId = "", string actionType = "")
    {
        bool firstDetection = false;
        lock (_pendingProblems)
        {
            _pendingProblems.Add((description, objectId, actionType));
            if (!CorruptionDetected)
            {
                CorruptionDetected = true;
                firstDetection = true;
            }
        }

        if (firstDetection)
        {
            Task.Run(() =>
            {
                Thread.Sleep(3000); // wait for pipe server to be ready
                EnableRecoveryMode();
                List<(string desc, string objectId, string actionType)> toSend;
                lock (_pendingProblems)
                {
                    toSend = new List<(string, string, string)>(_pendingProblems);
                }
                foreach (var (desc, objId, action) in toSend)
                    PipeClient.SendCommand($"recovery_problem|{objId}|{action}|{desc}");
                PipeClient.SendCommand("recovery_corruption_detected");
                _initialized = true;
            });
        }
        else if (_initialized)
        {
            PipeClient.SendCommand($"recovery_problem|{objectId}|{actionType}|{description}");
        }
    }

    [HarmonyPatch(typeof(KoGaMaPackageClient), "WorldObjectFactory")]
    [HarmonyFinalizer]
    private static Exception WorldObjectFactory_Finalizer(Exception __exception)
    {
        if (__exception is Il2CppInterop.Runtime.Il2CppException &&
            __exception.Message.Contains("KeyNotFoundException"))
        {
            MelonLogger.Warning("[RecoveryMode] Corrupted world data detected - missing 'OwnerActorNumber' key in WorldObjectFactory. Object will be skipped. This usually means the object has no owner assigned in the world data.");
            NotifyCorruption("Object skipped - missing owner data (OwnerActorNumber not found in world data)");
            return null;
        }
        return __exception;
    }

    [HarmonyPatch(typeof(MVWorldObjectClientManagerNetwork), "AddToWorldObjects")]
    [HarmonyPrefix]
    private static bool AddToWorldObjects_Prefix(MVWorldObjectClient wo)
    {
        if (wo == null)
        {
            MelonLogger.Warning("[RecoveryMode] Corrupted world data detected - WorldObjectFactory returned null, object skipped. This is likely caused by an unknown or unsupported WorldObjectType.");
            NotifyCorruption("Object skipped - null or unknown world object type");
            return false;
        }
        return true;
    }

    private static int _currentInitializingChildId = -1;

    [HarmonyPatch(typeof(MVWorldObjectClient), "Initialize")]
    [HarmonyPrefix]
    private static void MVWorldObjectClient_Initialize_Prefix(MVWorldObjectClient __instance)
    {
        _currentInitializingChildId = __instance?.Id ?? -1;
    }

    [HarmonyPatch(typeof(MVGroup), "Initialize")]
    [HarmonyFinalizer]
    private static Exception MVGroup_Initialize_Finalizer(Exception __exception)
    {
        if (__exception == null) return null;
        if (__exception is Il2CppInterop.Runtime.Il2CppException)
        {
            string firstLine = __exception.Message.Split('\n')[0].Trim();
            bool hasId = _currentInitializingChildId > 0;
            string description = hasId
                ? $"Object {_currentInitializingChildId} failed to initialize - {firstLine}."
                : $"An object failed to initialize - {firstLine}.";
            string action = hasId ? "remove" : "";

            MelonLogger.Warning($"[RecoveryMode] Object {_currentInitializingChildId} initialization failed: {firstLine}.");
            NotifyCorruption(description, _currentInitializingChildId.ToString(), action);
            return null;
        }
        return __exception;
    }

    private static readonly HashSet<IntPtr> _reportedBrokenLinks = new();

    [HarmonyPatch(typeof(ObjectLinkObjectScript), "UpdatePositions")]
    [HarmonyFinalizer]
    private static Exception UpdatePositions_Finalizer(ObjectLinkObjectScript __instance, Exception __exception)
    {
        if (__exception == null) return null;
        if (__exception is NullReferenceException ||
            (__exception is Il2CppInterop.Runtime.Il2CppException &&
             __exception.Message.Contains("NullReferenceException")))
        {
            if (_reportedBrokenLinks.Add(__instance.Pointer))
            {
                MelonLogger.Warning("[RecoveryMode] ObjectLink points to a missing or null object - skipping.");
                NotifyCorruption("Object link broken - target object is missing or failed to load");
            }
            return null;
        }
        return __exception;
    }



}
