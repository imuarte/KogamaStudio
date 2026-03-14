using Harmony;
using Il2Cpp;
using Il2CppAssets.Scripts.WorldObjectTypes.MVTextMsg;
using Il2CppBorodar.FarlandSkies.CloudyCrownPro.DotParams;
using Il2CppExitGames.Client.Photon;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppMV.WorldObject;
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
using MelonLoader;
using Newtonsoft.Json;
using System.Drawing;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using UnityEngine;


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
            MelonLogger.Msg(message);
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
            MelonLogger.Msg($"{id} {wo.type}");
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
            MelonLogger.Error($"[CommandHandler] Error: {ex.Message}");
        }
    }

    private static void ProcessCommand(string cmd)
    {
        int sep = cmd.IndexOf('|');
        string command = sep >= 0 ? cmd[..sep] : cmd;
        string param = sep >= 0 ? cmd[(sep + 1)..] : "";
        MelonCoroutines.Start(ExecuteCommand(command, param));
    }

    public static int RootGroupId => MVGameControllerBase.WOCM?.RootGroup?.Id ?? -1;

    public static CursorLockMode previousLockState = CursorLockMode.None;
    public static int targetWoId = -1;
    public static string targetPlayerName = null;
    public static bool previousVisible = true;
    public static MVWorldObjectClient savedWo = null;


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
                        MelonLogger.Error($"Path not found {path}");
                        break;
                    }

                    var files = Enumerable.Range(0, 69)
                        .Select(i => $"{path}/{i}.png")
                        .ToList();

                    MaterialsLoader.LoadTexture(files);

                    MelonLogger.Msg("[CommandHandler] test");

                    break;

                case "resourcepacks_reset":
                    MVGameControllerBase.MaterialLoader.SetMainTexture(DefaultMaterials.defaultMaterials, true);
                    break;
                case "imgui_menu":
                    if (param == "true")
                    {

                    }
                     break;
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

                case "generate_model":
                    if (!ModelBuilder.IsBuilding)
                    {
                        var cubes = ModelLoader.LoadModel(param);
                        if (cubes != null) 
                        { 
                            MelonCoroutines.Start(ModelBuilder.Build(cubes)); 
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
                case "explorer_refresh":
                    ExplorerManager.Refresh();
                    break;
                case "explorer_rename":
                {
                    var sep = param.IndexOf('|');
                    if (sep < 0) break;
                    if (!int.TryParse(param[..sep], out int renameId)) break;
                    string newName = param[(sep + 1)..];
                    int planetID = GameInfo.PlanetID;
                    if (planetID == -1) break;
                    var metaPath = ExplorerManager.GetMetaFilePath(planetID);
                    var meta = new System.Collections.Generic.Dictionary<int, ObjectMetaEntry>();
                    if (File.Exists(metaPath))
                        try { meta = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<int, ObjectMetaEntry>>(File.ReadAllText(metaPath)) ?? meta; } catch { }
                    if (string.IsNullOrEmpty(newName)) meta.Remove(renameId);
                    else meta[renameId] = new ObjectMetaEntry { Name = newName };
                    File.WriteAllText(metaPath, JsonConvert.SerializeObject(meta));
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
                                camMgr.cameraController.SetCamera((Il2Cpp.CameraType)modeId);
                                CameraModeModifier.Mode = (Il2Cpp.CameraType)modeId;
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
                    MelonCoroutines.Start(TranslationManager.StartLiveTranslation());
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
                case "properties_position_x":
                    {
                        var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(PropertiesManager.SelectedWOId);
                        if (wo == null) break;
                        var newPos = wo.Position;
                        newPos.x = float.Parse(param);
                        WorldObjectOperations.SetPosition(PropertiesManager.SelectedWOId, newPos);
                        break;
                    }
                case "properties_position_y":
                    {
                        var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(PropertiesManager.SelectedWOId);
                        if (wo == null) break;
                        var newPos = wo.Position;
                        newPos.y = float.Parse(param);
                        WorldObjectOperations.SetPosition(PropertiesManager.SelectedWOId, newPos);
                        break;
                    }
                case "properties_position_z":
                    {
                        var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(PropertiesManager.SelectedWOId);
                        if (wo == null) break;
                        var newPos = wo.Position;
                        newPos.z = float.Parse(param);
                        WorldObjectOperations.SetPosition(PropertiesManager.SelectedWOId, newPos);
                        break;
                    }
                case "properties_rotation_x":
                    currentEulerAngles.x = float.Parse(param);
                    WorldObjectOperations.SetRotation(PropertiesManager.SelectedWOId, Quaternion.Euler(currentEulerAngles));
                    break;
                case "properties_rotation_y":
                    currentEulerAngles.y = float.Parse(param);
                    WorldObjectOperations.SetRotation(PropertiesManager.SelectedWOId, Quaternion.Euler(currentEulerAngles));
                    break;
                case "properties_rotation_z":
                    currentEulerAngles.z = float.Parse(param);
                    WorldObjectOperations.SetRotation(PropertiesManager.SelectedWOId, Quaternion.Euler(currentEulerAngles));
                    break;
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
                    System.Action<string> managedAction = (error) => {
                        MelonLogger.Msg("Error: " + error);
                    };
                    MVGameControllerBase.OperationRequests.PublishPlanet(
                        DelegateSupport.ConvertDelegate<Il2CppSystem.Action<string>>(managedAction)
                    );
                    break;

                default:
                    MelonLogger.Msg($"[Commands] Unknown: {command}");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"[ExecuteCommand] Error: {ex.Message}");
        }
    }
}
