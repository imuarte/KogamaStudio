using HarmonyLib;
using Assets.Scripts.WorldObjectTypes.MVTextMsg;
using Borodar.FarlandSkies.CloudyCrownPro.DotParams;
using ExitGames.Client.Photon;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using MV.WorldObject;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Runtime.InteropServices;
using KogamaModFramework.Data;
using KogamaModFramework.Operations;
using KogamaStudio.Camera;
using KogamaStudio.Explorer;
using KogamaStudio.Clipboard;
using KogamaStudio.Generating.Models;
using KogamaStudio.Inventory;
using KogamaStudio.Objects;
using KogamaStudio.ResourcePacks.Materials;
using KogamaStudio.Tools.Build;
using KogamaStudio.Tools.Properties;
using KogamaStudio.Translator;

using Newtonsoft.Json;
using System.Drawing;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using UnityEngine;
using MV.Common;


namespace KogamaStudio;

public class CommandHandler
{
    internal static DesktopPlayModeController DesktopPlayModeController { get; set; } = null!;
    internal static DesktopEditModeController DesktopEditModeController { get; private set; } = null!;
    internal static EditorStateMachine EditorStateMachine { get; private set; } = null!;
    internal static CubeModelingStateMachine CubeModelingStateMachine { get; private set; } = null!;
    internal static EditorWorldObjectCreation EditorWorldObjectCreation { get; private set; } = null!;

    private static System.Collections.Generic.Dictionary<int, string> _originalTexts = new System.Collections.Generic.Dictionary<int, string>();

    internal static Vector3 currentEulerAngles = Vector3.zero;

    // tysm becko
    internal static void LoadReferences()
    {
        if (MVGameControllerBase.GameMode == MV.Common.MVGameMode.Edit)
        {
            DesktopEditModeController = MVGameControllerBase.EditModeUI.Cast<DesktopEditModeController>();
            EditorStateMachine = DesktopEditModeController.EditModeStateMachine;
            CubeModelingStateMachine = EditorStateMachine.cubeModelingStateMachine;
            EditorWorldObjectCreation = DesktopEditModeController.editorWorldObjectCreation;
        }
        else if (MVGameControllerBase.GameMode == MV.Common.MVGameMode.Play)
        {
            DesktopPlayModeController = MVGameControllerBase.PlayModeUI.Cast<DesktopPlayModeController>();
        }
    }

    private static void HandleCloneObject(int id)
    {
        var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);
        if (wo == null) return;
        MVGameControllerBase.OperationRequests.CloneWorldObjectTree(wo, false, false, false);
        TextCommand.NotifyUser($"Cloned {id}");
    }

    private static void HandleRemove(int id)
    {
        var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);
        string error = "";

        if (wo.Delete(MVGameControllerBase.WOCM, ref error))
        {
            string message = $"Removed {id}";
            KogamaStudio.Log.LogInfo(message);
            TextCommand.NotifyUser(message);
        }
    }

    private static System.Collections.Generic.Queue<(int id, string text)> _translateQueue =
new System.Collections.Generic.Queue<(int id, string text)>();


    private static Vector3 _lastPlayerPos = Vector3.zero;
    private static System.Collections.Generic.HashSet<int> _translatedIds = new System.Collections.Generic.HashSet<int>();

    private static bool _isTranslating = false;

    private static void HandleGetAllWoIds()
    {
        Il2CppSystem.Collections.Generic.HashSet<int> ids =
            new Il2CppSystem.Collections.Generic.HashSet<int>();
        MVGameControllerBase.WOCM.GetAllWoIds(75578, ids);

        foreach (int id in ids)
        {
            var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);

            TextCommand.NotifyUser($"{id} {wo.type}");
            KogamaStudio.Log.LogInfo($"{id} {wo.type}");
        }
    }

    public static void StartListening()
    {
        Task.Run(() => Listen());
    }

    public static void RefreshInventory()
    {
        var shopRepo = MVGameControllerBase.EditModeUI?.Cast<DesktopEditModeController>()?.PlayerShopInventoryRepository;
        if (shopRepo?.InventoryCategories == null) return;
        foreach (var kvp in shopRepo.InventoryCategories)
        {
            var catName = kvp.Value?.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim() ?? "";
            var items = shopRepo.GetInventoryItemsInCategorySlow(kvp.Value);
            if (items == null) continue;
            foreach (var item in items)
                PipeClient.SendCommand($"inventory_item|{item.itemID}|{item.name ?? ""}|{item.itemTypeID}|{(int)kvp.Key}|{catName}|{item.resellable}|{item.priceGold}|{item.purchased}|{item.authorProfileID}|{item.slotPosition}|{item.description ?? ""}");
        }
    }

    private static void Listen()
    {
        try
        {
            while (true)
            {
                using (var pipe = new NamedPipeServerStream("KogamaStudio"))
                {    
                    pipe.WaitForConnection();
                    using (var reader = new StreamReader(pipe))
                    {
                        string cmd = reader.ReadToEnd();
                        ProcessCommand(cmd);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            KogamaStudio.Log.LogError($"[CommandHandler] Error: {ex.Message}");
        }
    }

    private static void ProcessCommand(string cmd)
    {
        int sep = cmd.IndexOf('|');
        string command = sep >= 0 ? cmd[..sep] : cmd;
        string param = sep >= 0 ? cmd[(sep + 1)..] : "";
        KogamaStudioBehaviour.StartCo(ExecuteCommand(command, param));
    }

    public static int RootGroupId => MVGameControllerBase.WOCM?.RootGroup?.Id ?? -1;

    private static UnityEngine.GameObject _savedFocus = null;

    public static CursorLockMode previousLockState = CursorLockMode.None;
    public static int targetWoId = -1;
    public static string targetPlayerName = null;
    public static bool previousVisible = true;
    public static MVWorldObjectClient savedWo = null;

    private static int _lockCount;
    private static bool _lockDone;
    private static bool _transferDone;
    private static int _newGroupId = -1;
    private static bool _groupCreated;

    private static Il2CppSystem.EventHandler<OnHierarchyLockedEventArgs>     _lockHandler;
    private static Il2CppSystem.EventHandler<OnTransferWosResponseEventArgs> _transferHandler;
    private static Il2CppSystem.EventHandler<InitializedGameQueryDataEventArgs> _groupCreatedHandler;

    private static void SubscribeLock()
    {
        _lockHandler = DelegateSupport.ConvertDelegate<Il2CppSystem.EventHandler<OnHierarchyLockedEventArgs>>(
            new System.Action<Il2CppSystem.Object, OnHierarchyLockedEventArgs>((s, e) =>
            {
                _lockCount--;
                if (_lockCount <= 0)
                {
                    _lockDone = true;
                    MVGameControllerBase.WOCM.OnHierarchyLockedResponse =
                        MVGameControllerBase.WOCM.OnHierarchyLockedResponse - _lockHandler;
                }
            }));
        MVGameControllerBase.WOCM.OnHierarchyLockedResponse =
            MVGameControllerBase.WOCM.OnHierarchyLockedResponse + _lockHandler;
    }

    private static void SubscribeTransfer()
    {
        _transferHandler = DelegateSupport.ConvertDelegate<Il2CppSystem.EventHandler<OnTransferWosResponseEventArgs>>(
            new System.Action<Il2CppSystem.Object, OnTransferWosResponseEventArgs>((s, e) =>
            {
                _transferDone = true;
                MVGameControllerBase.WOCM.OnTransferWosResponse =
                    MVGameControllerBase.WOCM.OnTransferWosResponse - _transferHandler;
            }));
        MVGameControllerBase.WOCM.OnTransferWosResponse =
            MVGameControllerBase.WOCM.OnTransferWosResponse + _transferHandler;
    }

    private static void SubscribeGroupCreated()
    {
        _groupCreatedHandler = DelegateSupport.ConvertDelegate<Il2CppSystem.EventHandler<InitializedGameQueryDataEventArgs>>(
            new System.Action<Il2CppSystem.Object, InitializedGameQueryDataEventArgs>((s, e) =>
            {
                if (MVGameControllerBase.LocalPlayer.ActorNr == e.InstigatorActorNumber)
                {
                    _newGroupId = e.RootWO.Id;
                    _groupCreated = true;
                    KogamaStudio.Log.LogInfo($"[GroupObjects] Group created: {_newGroupId}");
                    MVGameControllerBase.Game.World.InitializedGameQueryData =
                        MVGameControllerBase.Game.World.InitializedGameQueryData - _groupCreatedHandler;
                }
            }));
        MVGameControllerBase.Game.World.InitializedGameQueryData =
            MVGameControllerBase.Game.World.InitializedGameQueryData + _groupCreatedHandler;
    }

    private static System.Collections.IEnumerator MoveToGroupCoroutine(int targetGroupId, int[] objectIds)
    {
        var rootId = MVGameControllerBase.WOCM.RootGroup.Id;
        bool isRoot = targetGroupId == rootId;

        KogamaStudio.Log.LogInfo($"[Move] Target: {targetGroupId}, isRoot: {isRoot}, objects: {string.Join(",", objectIds)}");

        _lockCount    = objectIds.Length + (isRoot ? 0 : 1);
        _lockDone     = false;
        _transferDone = false;

        SubscribeLock();

        if (!isRoot)
        {
            KogamaStudio.Log.LogInfo($"[Move] Locking target group {targetGroupId}");
            MVGameControllerBase.OperationRequests.LockHierarchy(targetGroupId, true);
        }

        foreach (int id in objectIds)
        {
            KogamaStudio.Log.LogInfo($"[Move] Locking object {id}");
            MVGameControllerBase.OperationRequests.LockHierarchy(id, true);
        }

        KogamaStudio.Log.LogInfo($"[Move] Waiting for {_lockCount} locks...");
        while (!_lockDone) yield return null;
        KogamaStudio.Log.LogInfo("[Move] All locks acquired");

        SubscribeTransfer();
        KogamaStudio.Log.LogInfo($"[Move] Calling TransferWorldObjectsToGroup({targetGroupId}, [{string.Join(",", objectIds)}])");
        MVGameControllerBase.OperationRequests.TransferWorldObjectsToGroup(
            targetGroupId, new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int>(objectIds));

        KogamaStudio.Log.LogInfo("[Move] Waiting for transfer response...");

        float timeout = 0f;
        while (!_transferDone)
        {
            timeout += UnityEngine.Time.deltaTime;
            if (timeout > 10f)
            {
                KogamaStudio.Log.LogInfo("[Move] TIMEOUT - transfer response never came");
                yield break;
            }
            yield return null;
        }
        KogamaStudio.Log.LogInfo("[Move] Transfer done!");

        foreach (int id in objectIds)
            MVGameControllerBase.OperationRequests.LockHierarchy(id, false);
        if (!isRoot)
            MVGameControllerBase.OperationRequests.LockHierarchy(targetGroupId, false);
        MVGameControllerBase.OperationRequests.TransferOwnership(targetGroupId, 0, null);

        Explorer.ExplorerManager.Refresh();
        KogamaStudio.Log.LogInfo("[Move] Finished");
        PipeClient.SendCommand("explorer_deselect_all");
    }

    private static System.Collections.IEnumerator GroupObjectsCoroutine(int[] objectIds, UnityEngine.Vector3 position, int parentGroupId)
    {
        _lockCount    = objectIds.Length;
        _lockDone     = false;
        _transferDone = false;
        _groupCreated = false;
        _newGroupId   = -1;

        SubscribeLock();
        foreach (int id in objectIds)
            MVGameControllerBase.OperationRequests.LockHierarchy(id, true);
        while (!_lockDone) yield return null;

        SubscribeGroupCreated();
        MVGameControllerBase.OperationRequests.RequestBuiltInItem(
            BuiltInItem.Group,
            parentGroupId,
            new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object>(),
            position,
            UnityEngine.Quaternion.identity,
            UnityEngine.Vector3.one,
            true,
            true
        );
        while (!_groupCreated) yield return null;

        SubscribeTransfer();
        MVGameControllerBase.OperationRequests.TransferWorldObjectsToGroup(
            _newGroupId, new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int>(objectIds));

        while (!_transferDone) yield return null;

        foreach (int id in objectIds)
            MVGameControllerBase.OperationRequests.LockHierarchy(id, false);
        MVGameControllerBase.OperationRequests.TransferOwnership(_newGroupId, 0, null);

        KogamaStudio.Log.LogInfo($"[GroupObjects] Grouped {objectIds.Length} objects into group {_newGroupId}");
        Explorer.ExplorerManager.Refresh();
        PipeClient.SendCommand("explorer_deselect_all");
    }

    public static System.Collections.IEnumerator ExecuteCommand(string command, string param)
    {
        yield return null;

        try { 
            switch (command)
            {
                case "resourcepacks_load":

                    string path = Path.Combine(PathHelper.GetPath(), "ResourcePacks", param, "materials");

                    if (!Directory.Exists(path))
                    {
                        KogamaStudio.Log.LogError($"Path not found {path}");
                        break;
                    }

                    var files = Enumerable.Range(0, 69)
                        .Select(i => $"{path}/{i}.png")
                        .ToList();

                    MaterialsLoader.LoadTexture(files);

                    KogamaStudio.Log.LogInfo("[CommandHandler] test");

                    break;

                case "resourcepacks_reset":
                    MVGameControllerBase.MaterialLoader.SetMainTexture(DefaultMaterials.defaultMaterials, true);
                    break;
                case "imgui_menu":
                    if (param == "true")
                    {

                    }
                     break;
                case "viewport_cursor":
                {
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (param == "true")
                    {
                        _savedFocus = es?.currentSelectedGameObject;
                        es?.SetSelectedGameObject(null);
                    }
                    else
                    {
                        es?.SetSelectedGameObject(_savedFocus);
                    }
                    break;
                }
                case "option_no_build_limit":
                    NoBuildLimit.Enabled = param == "true";
                    break;
                case "option_single_side_painting":
                    SingleSidePainting.Enabled = param == "true";
                    break;
                case "option_anti_afk":
                    AntiAFK.Enabled = param == "true";
                    break;
                case "option_destructibles_unlock":
                    DestructiblesUnlock.Unlocked = param == "true";
                    break;
                case "option_blue_mode":
                    BlueModeToggle.BlueModeEnabled = param == "true";
                    break;
                // GRID SIZE
                case "option_custom_grid_size_enabled":
                    CustomGrid.Enabled = param == "true";
                    break;
                case "option_custom_grid_size":
                    CustomGrid.GridSize = float.Parse(param);
                    break;
                // ROTATION STEP
                case "option_custom_rot_step_enabled":
                    RotationStep.Enabled = param == "true";
                    break;
                case "option_custom_rot_step_size":
                    RotationStep.Step = float.Parse(param);
                    break;
                // SPEED
                case "option_custom_speed_enabled":
                    EditModeSpeed.MultiplierEnabled = param == "true";
                    break;
                case "option_custom_speed_value":
                    EditModeSpeed.Multiplier = float.Parse(param);
                    break;
                // UNLIMITED CONFIG
                case "option_unlimited_config_enabled":
                    UnlimitedConfig.Enabled = param == "true";
                    break;
                case "option_unlimited_config_min":
                    UnlimitedConfig.MinValue = float.Parse(param);
                    break;
                case "option_unlimited_config_max":
                    UnlimitedConfig.MaxValue = float.Parse(param);
                    break;
                // CUSTOM MODEL SCALE
                case "option_custom_model_scale_enabled":
                    CustomModelScale.Enabled = param == "true";
                    break;
                case "option_custom_model_scale_value":
                    CustomModelScale.Scale = float.Parse(param);
                    break;
                // CUSTOM WO SCALE
                case "option_custom_wo_scale_enabled":
                    CustomWOScale.Enabled = param == "true";
                    if (param == "true") CustomWOScale.RequestNewGroupIfNecessary();
                    break;
                case "option_custom_wo_scale_x":
                    CustomWOScale.Scale = new Vector3(float.Parse(param, System.Globalization.CultureInfo.InvariantCulture), CustomWOScale.Scale.y, CustomWOScale.Scale.z);
                    if (CustomWOScale.Enabled) CustomWOScale.RequestNewGroupIfNecessary();
                    break;
                case "option_custom_wo_scale_y":
                    CustomWOScale.Scale = new Vector3(CustomWOScale.Scale.x, float.Parse(param, System.Globalization.CultureInfo.InvariantCulture), CustomWOScale.Scale.z);
                    if (CustomWOScale.Enabled) CustomWOScale.RequestNewGroupIfNecessary();
                    break;
                case "option_custom_wo_scale_z":
                    CustomWOScale.Scale = new Vector3(CustomWOScale.Scale.x, CustomWOScale.Scale.y, float.Parse(param, System.Globalization.CultureInfo.InvariantCulture));
                    if (CustomWOScale.Enabled) CustomWOScale.RequestNewGroupIfNecessary();
                    break;

                case "generate_model":
                    if (!ModelBuilder.IsBuilding)
                    {
                        var cubes = ModelLoader.LoadModel(param);
                        if (cubes != null) 
                        { 
                            KogamaStudioBehaviour.StartCo(ModelBuilder.Build(cubes)); 
                        }
                    }
                    break;
                case "generate_cancel":
                    
                    ModelBuilder.CancelGeneration = true;
                    break;
                case "generate_save_model_to_file":

                    break;

                case "objects_wo_id":
                    targetWoId = int.Parse(param);
                    break;
                case "objects_get_all_wo_ids":
                    HandleGetAllWoIds();
                    break;
                case "objects_clone":
                    HandleCloneObject(targetWoId); 
                    break;
                case "objects_remove":
                    HandleRemove(targetWoId);
                    break;
                case "objects_visible":
                    break;
                case "explorer_create_group":
                {
                    // format: groupId|posX|posY|posZ|rotX|rotY|rotZ|scaleX|scaleY|scaleZ|localOwner
                    var p = param.Split('|');
                    float F(int i, float def = 0f) => p.Length > i && float.TryParse(p[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : def;
                    int groupId = p.Length > 0 && int.TryParse(p[0], out int g) ? g : EditorStateMachine.ParentGroupID;
                    var pos   = new UnityEngine.Vector3(F(1), F(2), F(3));
                    var rot   = UnityEngine.Quaternion.Euler(F(4), F(5), F(6));
                    var scale = new UnityEngine.Vector3(F(7, 1f), F(8, 1f), F(9, 1f));
                    bool lo   = p.Length > 10 && p[10] == "1";
                    MVGameControllerBase.OperationRequests.RequestBuiltInItem(
                        BuiltInItem.Group, groupId,
                        new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object>(),
                        pos, rot, scale, lo, true);
                    break;
                }
                case "explorer_create_cubemodel":
                {
                    // format: posX|posY|posZ|rotX|rotY|rotZ|scale|materialId
                    // Always uses root group (like the game's own OnAddNewPrototype)
                    var p = param.Split('|');
                    float F(int i, float def = 0f) => p.Length > i && float.TryParse(p[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : def;
                    var pos   = new UnityEngine.Vector3(F(0), F(1), F(2));
                    var rot   = UnityEngine.Quaternion.Euler(F(3), F(4), F(5));
                    float scale = F(6, 1f);
                    int matId = p.Length > 7 && int.TryParse(p[7], out int m) ? m
                                : (int)CubeModelingStateMachine.CurrentMaterialId;
                    int profileId = MVGameControllerBase.Game?.LocalPlayer?.ProfileID
                                    ?? GameInfo.ProfileID;
                    int rootId = MVGameControllerBase.WOCM.RootGroup.Id;
                    var customData = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object>();
                    customData.Add(BoxInt(1), BoxFloat(scale));
                    customData.Add(BoxInt(2), BoxInt(matId));
                    customData.Add(BoxInt(3), BoxInt(profileId));
                    MVGameControllerBase.OperationRequests.RequestBuiltInItem(
                        BuiltInItem.CubeModel, rootId, customData,
                        pos, rot, UnityEngine.Vector3.one * scale, false, true);
                    break;
                }
                case "explorer_set_order":
                    ExplorerManager.SetOrder(param);
                    break;
                case "explorer_refresh":
                    ExplorerManager.Refresh();
                    break;
                case "explorer_move_to_group":
                {
                    var sep2 = param.IndexOf('|');
                    if (sep2 < 0) break;
                    if (!int.TryParse(param[..sep2], out int targetGroupId)) break;
                    var ids = param[(sep2 + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(s => int.Parse(s.Trim())).ToArray();
                    if (ids.Length == 0) break;
                    KogamaStudioBehaviour.StartCo(MoveToGroupCoroutine(targetGroupId, ids));
                    break;
                }
                case "explorer_group_selection":
                {
                    var ids = param.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => int.Parse(s.Trim())).ToArray();
                    if (ids.Length == 0) break;
                    var wocm = MVGameControllerBase.WOCM;
                    var pos = UnityEngine.Vector3.zero;
                    int validCount = 0;
                    foreach (int id in ids)
                    {
                        var wo = wocm.GetWorldObjectClient(id);
                        if (wo != null) { pos += wo.Position; validCount++; }
                    }
                    if (validCount > 0) pos /= validCount;
                    int parentGroup = wocm.RootGroup?.Id ?? -1;
                    KogamaStudioBehaviour.StartCo(GroupObjectsCoroutine(ids, pos, parentGroup));
                    break;
                }
                case "explorer_rename":
                {
                    var sep2 = param.IndexOf('|');
                    if (sep2 < 0) break;
                    if (!int.TryParse(param[..sep2], out int renameId)) break;
                    ExplorerManager.Rename(renameId, param[(sep2 + 1)..]);
                    break;
                }
                case "explorer_select":
                    targetWoId = int.Parse(param);
                    if (Objects.WOIdGetter.Instance != null)
                    {
                        Objects.WOIdGetter.Instance.ExitGroupToRoot();
                        Objects.WOIdGetter.Instance.SelectWO(targetWoId);
                    }
                    else
                        PropertiesManager.SendProperties(targetWoId);
                    break;
                case "camera_freecam_enable":
                    Freecam.Enable();
                    break;
                case "camera_freecam_disable":
                    Freecam.Disable();
                    break;
                case "camera_freecam_speed":
                    Freecam.Speed = float.Parse(param);
                    break;
                case "camera_freecam_sensitivity":
                    Freecam.Sensitivity = float.Parse(param);
                    break;
                case "camera_freecam_require_rmb":
                    Freecam.RequireRightClick = param == "true";
                    break;
                case "camera_set_mode":
                    if (int.TryParse(param, out int modeId))
                    {
                        var camMgr = MVGameControllerBase.MainCameraManager;
                        if (camMgr?.cameraController != null)
                            try
                            {
                                camMgr.cameraController.SetCamera((CameraType)modeId);
                                CameraModeModifier.Mode = (CameraType)modeId;
                                CameraModeModifier.Enabled = true;
                            }
                            catch (Exception) { PipeClient.SendCommand("camera_mode_unavailable"); }
                    }
                    break;
                case "camera_mode_reset":
                    CameraModeModifier.Enabled = false;
                    break;
                // TRANSLATOR
                // translate own messages
                case "translate_own_messages_enabled":
                    AddLinePatch.TranslateOwnMessagesEnabled = param == "true";
                    break;
                case "translate_own_messages_language":
                    AddLinePatch.TranslateOwnMessagesLanguage = param;
                    break;
                // translate text cubes
                case "translate_text_cubes_language":
                    AddLinePatch.TranslateTextCubesLanguage = param;
                    break;
                case "translate_text_cubes_translate":
                    TranslationManager.BackupTextCubes();
                    KogamaStudioBehaviour.StartCo(TranslationManager.StartLiveTranslation());
                    break;

                case "translate_text_cubes_enabled":
                    AddLinePatch.TranslateTextCubesEnabled = param == "true";

                    if (param == "true")
                    {
                        AddLinePatch.TranslateOtherMessagesEnabled = true;
                        TranslationManager.BackupTextCubes();
                        _lastPlayerPos = MVGameControllerBase.LocalPlayer.WoId != -1 ?
                            MVGameControllerBase.WOCM?.GetWorldObjectClient(MVGameControllerBase.LocalPlayer.WoId)?.Position ?? Vector3.zero
                            : Vector3.zero;
                    }
                    else
                    {
                        AddLinePatch.TranslateOtherMessagesEnabled = false;
                        TranslationManager.RestoreTextCubes();
                    }
                    break;

                // PROPERTIES
                case "history_restore_position":
                {
                    var p = param.Split('|');
                    if (p.Length < 4 || !int.TryParse(p[0], out int restoreId)) break;
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    float rx = float.Parse(p[1], inv);
                    float ry = float.Parse(p[2], inv);
                    float rz = float.Parse(p[3], inv);
                    WorldObjectOperations.SetPosition(restoreId, new Vector3(rx, ry, rz));
                    break;
                }
                case "history_restore_rotation":
                {
                    var p = param.Split('|');
                    if (p.Length < 5 || !int.TryParse(p[0], out int restoreId)) break;
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    float qx = float.Parse(p[1], inv);
                    float qy = float.Parse(p[2], inv);
                    float qz = float.Parse(p[3], inv);
                    float qw = float.Parse(p[4], inv);
                    WorldObjectOperations.SetRotation(restoreId, new Quaternion(qx, qy, qz, qw));
                    break;
                }
                case "properties_remove":
                    HandleRemove(PropertiesManager.SelectedWOId);
                    break;
                case "properties_position":
                    {
                        var ps = param.Split('|');
                        if (ps.Length == 3)
                        {
                            var ci = System.Globalization.CultureInfo.InvariantCulture;
                            WorldObjectOperations.SetPosition(PropertiesManager.SelectedWOId,
                                new Vector3(float.Parse(ps[0], ci), float.Parse(ps[1], ci), float.Parse(ps[2], ci)));
                        }
                        break;
                    }
                case "properties_rotation":
                    {
                        var ps = param.Split('|');
                        if (ps.Length == 3)
                        {
                            var ci = System.Globalization.CultureInfo.InvariantCulture;
                            currentEulerAngles = new Vector3(float.Parse(ps[0], ci), float.Parse(ps[1], ci), float.Parse(ps[2], ci));
                            WorldObjectOperations.SetRotation(PropertiesManager.SelectedWOId, Quaternion.Euler(currentEulerAngles));
                        }
                        break;
                    }
                case "properties_history_rotation":
                {
                    var parts = param.Split('|');
                    if (parts.Length == 7 && int.TryParse(parts[0], out int histRotId))
                    {
                        var oldQ = Quaternion.Euler(
                            float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
                        var newQ = Quaternion.Euler(
                            float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(parts[6], System.Globalization.CultureInfo.InvariantCulture));
                        PipeClient.SendCommand(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "history_rotation|{0}|{1:F5}|{2:F5}|{3:F5}|{4:F5}|{5:F5}|{6:F5}|{7:F5}|{8:F5}",
                            histRotId,
                            oldQ.x, oldQ.y, oldQ.z, oldQ.w,
                            newQ.x, newQ.y, newQ.z, newQ.w));
                    }
                    break;
                }
                // CLIPBOARD
                case "clipboard_clear":
                    Clipboard.ClipboardManager.Clear();
                    break;
                case "clipboard_copy_model":
                    Clipboard.ClipboardManager.CopyPropertiesModel();
                    break;
                case "clipboard_save_to_file":
                    Clipboard.ClipboardManager.SaveToFile(param);
                    break;
                case "clipboard_load_from_file":
                    Clipboard.ClipboardManager.LoadFromFile(param);
                    break;
                case "clipboard_paste_model":
                    Clipboard.ClipboardManager.PasteEditedModel();
                    break;
                // editor
                // offset
                case "clipboard_offset_x":
                    Clipboard.ClipboardManager.OffsetX = int.Parse(param);
                    break;
                case "clipboard_offset_y":
                    Clipboard.ClipboardManager.OffsetY = int.Parse(param);
                    break;
                case "clipboard_offset_z":
                    Clipboard.ClipboardManager.OffsetZ = int.Parse(param);
                    break;
                // rotation
                case "clipboard_rotation_x":
                    Clipboard.ClipboardManager.RotationX = int.Parse(param);
                    break;
                case "clipboard_rotation_y":
                    Clipboard.ClipboardManager.RotationY = int.Parse(param);
                    break;
                case "clipboard_rotation_z":
                    Clipboard.ClipboardManager.RotationZ = int.Parse(param);
                    break;
                // mirror
                case "clipboard_mirror_x":
                    Clipboard.ClipboardManager.MirrorX = param == "true";
                    break;
                case "clipboard_mirror_y":
                    Clipboard.ClipboardManager.MirrorY = param == "true";
                    break;
                case "clipboard_mirror_z":
                    Clipboard.ClipboardManager.MirrorZ = param == "true";
                    break;
                // clip min
                case "clipboard_clip_min_x":
                    Clipboard.ClipboardManager.ClipMinX = int.Parse(param);
                    break;
                case "clipboard_clip_min_y":
                    Clipboard.ClipboardManager.ClipMinY = int.Parse(param);
                    break;
                case "clipboard_clip_min_z":
                    Clipboard.ClipboardManager.ClipMinZ = int.Parse(param);
                    break;
                // clip max
                case "clipboard_clip_max_x":
                    Clipboard.ClipboardManager.ClipMaxX = int.Parse(param);
                    break;
                case "clipboard_clip_max_y":
                    Clipboard.ClipboardManager.ClipMaxY = int.Parse(param);
                    break;
                case "clipboard_clip_max_z":
                    Clipboard.ClipboardManager.ClipMaxZ = int.Parse(param);
                    break;
                // preview
                case "clipboard_preview_paste_model":
                    Clipboard.ClipboardManager.Preview = param == "true";
                    Clipboard.ClipboardManager.ShowPreview();
                    Clipboard.ClipboardManager.Preview = param == "true";
                    break;
                case "clipboard_paste_cancel":
                    Clipboard.ClipboardManager.CancelPaste();
                    break;
                case "inventory_refresh":
                    RefreshInventory();
                    break;
                case "inventory_remove":
                {
                    if (int.TryParse(param, out int removeId))
                        MVGameControllerBase.OperationRequests.RemoveItemFromInventory(removeId);
                    break;
                }
                case "inventory_add_object":
                {
                    if (int.TryParse(param, out int woId))
                        InventoryManager.AddItem(woId);
                    break;
                }
                case "world_place_item":
                {
                    if (!int.TryParse(param, out int placeItemId)) break;
                    KogamaStudio.RunOnMainThread(() =>
                    {
                        var cam = MVGameControllerBase.MainCameraManager?.MainCamera;
                        var pos = cam != null ? cam.transform.position + cam.transform.forward * 5f : UnityEngine.Vector3.zero;
                        MVGameControllerBase.OperationRequests.AddItemToWorld(placeItemId, RootGroupId, pos, UnityEngine.Quaternion.identity, true, true, false);
                    });
                    break;
                }
                case "world_place_item_in_group":
                {
                    var sep2 = param.IndexOf('|');
                    if (sep2 < 0) break;
                    if (!int.TryParse(param[..sep2], out int placeItemId)) break;
                    if (!int.TryParse(param[(sep2 + 1)..], out int groupId)) break;
                    KogamaStudio.RunOnMainThread(() =>
                    {
                        var cam = MVGameControllerBase.MainCameraManager?.MainCamera;
                        var pos = cam != null ? cam.transform.position + cam.transform.forward * 5f : UnityEngine.Vector3.zero;
                        MVGameControllerBase.OperationRequests.AddItemToWorld(placeItemId, groupId, pos, UnityEngine.Quaternion.identity, true, true, false);
                    });
                    break;
                }
                case "recovery_disable_loading_screen":
                    RecoveryMode.DisableLoadingScreen();
                    break;
                case "recovery_target_item_id":
                    RecoveryMode.TargetItemId = int.Parse(param);
                    break;
                case "recovery_remove_item":
                    InventoryManager.RemoveItem(RecoveryMode.TargetItemId);
                    break;
                case "recovery_remove_category":
                {
                    if (!int.TryParse(param, out int catId)) break;
                    var shopRepo = MVGameControllerBase.EditModeUI?.Cast<DesktopEditModeController>()?.PlayerShopInventoryRepository;
                    if (shopRepo?.InventoryCategories == null) break;
                    foreach (var kvp in shopRepo.InventoryCategories)
                    {
                        if ((int)kvp.Key != catId) continue;
                        var items = shopRepo.GetInventoryItemsInCategorySlow(kvp.Value);
                        if (items != null) foreach (var item in items) InventoryManager.RemoveItem(item.itemID);
                        break;
                    }
                    break;
                }
                case "recovery_remove_object":
                    if (int.TryParse(param, out int removeWoId))
                        HandleRemove(removeWoId);
                    break;
                case "recovery_teleport_to_object":
                    if (int.TryParse(param, out int teleportWoId))
                    {
                        var recoveryTargetWo = MVGameControllerBase.WOCM?.GetWorldObjectClient(teleportWoId);
                        var recoveryLocalWo = MVGameControllerBase.WOCM?.GetWorldObjectClient(MVGameControllerBase.LocalPlayer.WoId);
                        if (recoveryTargetWo != null && recoveryLocalWo != null)
                            recoveryLocalWo.Position = recoveryTargetWo.Position;
                    }
                    break;
                case "teleport_player":
                    var playerWo = WorldObjectOperations.GetObject(int.Parse(param));
                    var localPlayerWo = WorldObjectOperations.GetObject(MVGameControllerBase.LocalPlayer.WoId);

                    if (playerWo != null && localPlayerWo != null)
                        localPlayerWo.Position = playerWo.Position;

                    break;
                case "camera_fov":
                    FovModifier.Enabled = true;
                    FovModifier.Fov = float.Parse(param);
                    break;
                case "camera_fov_reset":
                    FovModifier.Enabled = false;
                    break;
                case "camera_zoom":
                    ZoomModifier.Enabled = true;
                    ZoomModifier.ZoomLevel = float.Parse(param, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "camera_zoom_reset":
                    ZoomModifier.Enabled = false;
                    break;
                case "set_resolution":
                {
                    var dims = param.Split('x');
                    if (dims.Length == 2 && int.TryParse(dims[0], out int rW) && int.TryParse(dims[1], out int rH))
                        Screen.SetResolution(rW, rH, false);
                    break;
                }
                case "set_resolution_reset":
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
                    break;
                case "camera_distance":
                    CameraDistanceModifier.distance = float.Parse(param);
                    CameraDistanceModifier.ApplyChanges();
                    break;
                case "camera_distance_reset":
                    CameraDistanceModifier.distance = CameraDistanceModifier.defaultDistance;
                    CameraDistanceModifier.ApplyChanges();
                    break;
                case "test":
                    break;

                default:
                    KogamaStudio.Log.LogInfo($"[Commands] Unknown: {command}");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            KogamaStudio.Log.LogError($"[ExecuteCommand] Error: {ex.Message}");
        }
    }

    private static IntPtr _int32Klass  = IntPtr.Zero;
    private static IntPtr _singleKlass = IntPtr.Zero;

    private static unsafe IntPtr FindKlass(ref IntPtr cache, string ns, string name)
    {
        if (cache != IntPtr.Zero) return cache;
        IntPtr domain = IL2CPP.il2cpp_domain_get();
        uint size = 0;
        IntPtr* assemblies = IL2CPP.il2cpp_domain_get_assemblies(domain, ref size);
        for (uint i = 0; i < size; i++)
        {
            IntPtr image = IL2CPP.il2cpp_assembly_get_image(assemblies[i]);
            IntPtr klass = IL2CPP.il2cpp_class_from_name(image, ns, name);
            if (klass != IntPtr.Zero) { cache = klass; return klass; }
        }
        return IntPtr.Zero;
    }

    private static unsafe Il2CppSystem.Object BoxInt(int value)
    {
        IntPtr klass = FindKlass(ref _int32Klass, "System", "Int32");
        if (klass == IntPtr.Zero) return null;
        return new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(klass, (IntPtr)(&value)));
    }

    private static unsafe Il2CppSystem.Object BoxFloat(float value)
    {
        IntPtr klass = FindKlass(ref _singleKlass, "System", "Single");
        if (klass == IntPtr.Zero) return null;
        return new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(klass, (IntPtr)(&value)));
    }
}
