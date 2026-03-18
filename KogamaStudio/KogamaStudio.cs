using KogamaStudio.Camera;
using KogamaStudio.Clipboard;
using KogamaStudio.Tools;
using KogamaStudio.Tools.Properties;
using KogamaStudio.Translator;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace KogamaStudio
{
    [BepInPlugin("com.amuarte.kogamastudio", "KogamaStudio", KSVersion.Value)]
    public class KogamaStudio : BasePlugin
    {
        internal static new ManualLogSource Log;
        public static bool gameInitialized = false;
        private static bool harmonyPatched = false;

        public override void Load()
        {
            Log = base.Log;

            var harmony = new HarmonyLib.Harmony("com.amuarte.kogamastudio");
            harmony.PatchAll();

            if (harmonyPatched) return;

            DllLoader.Load("KogamaStudio-ImGui-Hook.dll");
            CommandHandler.StartListening();
            DirectoryManager.Initialize();

            var methods = typeof(SelectionController).GetMethods()
    .Where(m => m.Name == "SelectWO");
            foreach (var m in methods)
                Log.LogInfo($"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");

            AddComponent<KogamaStudioBehaviour>();
        }

        internal static bool _wasPasting = false;
        private static System.Collections.Concurrent.ConcurrentQueue<System.Action> _mainThreadQueue = new();
        internal static int _lastSentPlaced = -1;
        internal static int _lastObjectCount = -1;
        internal static float _explorerRefreshAt = -1f;
        internal static bool _lastCursorState = false;

        public static void RunOnMainThread(System.Action action) => _mainThreadQueue.Enqueue(action);

        internal static void ProcessMainThread()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
                action();
        }
    }

    public class KogamaStudioBehaviour : MonoBehaviour
    {
        internal static KogamaStudioBehaviour Instance;
        private bool? _lastFullscreen = null;

        void Awake() { Instance = this; }

        internal static void StartCo(System.Collections.IEnumerator routine)
        {
            Instance.StartCoroutine(routine);
        }

        void Update()
        {
            KogamaStudio.ProcessMainThread();

            bool cursorState = Cursor.visible || Cursor.lockState == CursorLockMode.None;
            if (cursorState != KogamaStudio._lastCursorState)
            {
                KogamaStudio._lastCursorState = cursorState;
                PipeClient.SendCommand($"cursor|{(cursorState ? "true" : "false")}");
            }

            bool fs = Screen.fullScreen;
            if (fs != _lastFullscreen)
            {
                _lastFullscreen = fs;
                PipeClient.SendCommand($"fullscreen|{(fs ? "true" : "false")}");
            }

            if (!KogamaStudio.gameInitialized)
            {
                if (MVGameControllerBase.IsInitialized)
                {
                    KogamaStudio.Log.LogInfo("KogamaStudio loaded!");


                    KogamaStudio.gameInitialized = true;
                    CommandHandler.LoadReferences();
                    if (DllLoader.ImGuiLoaded)
                    {
                        PipeClient.SendCommand("game_initialized");
                        TextCommand.NotifyUser($"<b>KogamaStudio</b> v{KSVersion.Value} loaded!\nPress <b>F2</b> to open menu.");
                    }
                    else
                    {
                        string err = DllLoader.LoadError ?? "Unknown error";
                        TextCommand.NotifyUser($"<b>KogamaStudio</b> v{KSVersion.Value}: GUI failed to load!\n{err}\nCheck: %LOCALAPPDATA%\\KogamaStudio\\Logs\\imgui-hook.log");
                        KogamaStudio.Log.LogError($"ImGui hook DLL not loaded: {err}");
                    }
                    Players.PlayerList.Start();
                    GlLineDrawer.Init();
                    GameInfo.Init();
                    PipeClient.SendCommand("inventory_clear");
                    CommandHandler.RefreshInventory();
                    Explorer.ExplorerManager.RefreshWhenReady();
                }

            }

            if (KogamaStudio.gameInitialized)
            {
                var wocm = MVGameControllerBase.WOCM;
                if (wocm?.worldObjects != null)
                {
                    int count = wocm.worldObjects.Count;
                    if (count != KogamaStudio._lastObjectCount)
                    {
                        KogamaStudio._lastObjectCount = count;
                        KogamaStudio._explorerRefreshAt = Time.time + 1f;
                    }
                    if (KogamaStudio._explorerRefreshAt > 0f && Time.time >= KogamaStudio._explorerRefreshAt)
                    {
                        KogamaStudio._explorerRefreshAt = -1f;
                        Explorer.ExplorerManager.Refresh();
                    }
                }

                bool pasting = KogamaModFramework.Operations.CubeOperations.IsBuilding;
                if (pasting)
                {
                    float progress = KogamaModFramework.Operations.CubeOperations.Progress;
                    int total = ClipboardManager.LastPasteTotalCubes;
                    int placed = (int)(progress * total);
                    if (placed != KogamaStudio._lastSentPlaced)
                    {
                        KogamaStudio._lastSentPlaced = placed;
                        int batchSize = KogamaModFramework.Operations.CubeOperations.BatchSize;
                        float timePerBatch = KogamaModFramework.Operations.CubeOperations.FrameDelay / 60f;
                        int remainingBatches = (total - placed + batchSize - 1) / batchSize;
                        float timeLeft = remainingBatches * timePerBatch;
                        PipeClient.SendCommand($"clipboard_paste_progress|{placed}|{total}|{timeLeft:F1}");
                    }
                }
                else if (KogamaStudio._wasPasting)
                {
                    KogamaStudio._lastSentPlaced = -1;
                    PipeClient.SendCommand("clipboard_paste_done");
                }
                KogamaStudio._wasPasting = pasting;

                var mgr = MVGameControllerBase.MainCameraManager;
                var cam = mgr?.MainCamera;
                if (cam != null)
                {
                    GlLineDrawer.BeginFrame();
                    if (ClipboardManager.Preview)
                    {
                        var cubes = ClipboardManager.GetTransformedCubes();
                        if (cubes != null)
                        {
                            var baseObj = ModelProperties.CubeModelBase;
                            var t = baseObj?.transform;
                            Vector3? pos = null;
                            if (t != null)
                                pos = t.position - t.rotation * (t.localScale * 0.5f);
                            GlLineDrawer.Cubes(cubes, Color.yellow,
                                position: pos,
                                scale: t?.localScale,
                                rotation: t?.rotation);
                        }
                    }
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                PipeClient.SendCommand("key_down|F2");
            }

            //recovery mode
            if (UnityEngine.Input.GetKeyDown(KeyCode.F8))
            {
                RecoveryMode.EnableRecoveryMode();
            }

            if (Time.frameCount % 30 == 0)
            {
                var _cam = MVGameControllerBase.MainCameraManager?.MainCamera;
                if (_cam != null)
                {
                    var _p = _cam.transform.position;
                    PipeClient.SendCommand($"camera_pos|{_p.x:F2}|{_p.y:F2}|{_p.z:F2}");
                }
            }

            if (AddLinePatch.TranslateTextCubesEnabled)
            {
                TranslationManager.UpdateLiveTranslation();
            }

            if (MessageTranslator.Instance.TranslationReady)
            {
                if (AddLinePatch.CurrentTranslationMode == AddLinePatch.MessageTranslationType.OwnMessages)
                {
                    AddLinePatch.SendMessageControlInstance.SendChatMessage(MessageTranslator.Instance.LastTranslation);
                }
                AddLinePatch.CurrentTranslationMode = AddLinePatch.MessageTranslationType.None;
            }
        }

        private static bool _wasZooming = false;
        private static float _savedFov = 60f;

        void LateUpdate()
        {
            Freecam.Update();

            var mgr = MVGameControllerBase.MainCameraManager;
            if (mgr == null) return;

            if (Freecam.IsEnabled)
            {
                if (mgr.MainCamera != null)
                {
                    mgr.MainCamera.transform.position = Freecam.Position;
                    mgr.MainCamera.transform.rotation = Freecam.Rotation;
                }
                if (mgr.SecondaryCamera != null)
                {
                    mgr.SecondaryCamera.transform.position = Freecam.Position;
                    mgr.SecondaryCamera.transform.rotation = Freecam.Rotation;
                }
                if (mgr.TertiaryCamera != null)
                {
                    mgr.TertiaryCamera.transform.position = Freecam.Position;
                    mgr.TertiaryCamera.transform.rotation = Freecam.Rotation;
                }
            }

            float baseFov = FovModifier.Enabled ? FovModifier.Fov : 60f;
            if (ZoomModifier.Enabled)
            {
                if (!_wasZooming) { _savedFov = mgr.FieldOfView; _wasZooming = true; }
                mgr.FieldOfView = baseFov / ZoomModifier.ZoomLevel;
            }
            else
            {
                if (_wasZooming) { mgr.FieldOfView = FovModifier.Enabled ? FovModifier.Fov : _savedFov; _wasZooming = false; }
                else if (FovModifier.Enabled) mgr.FieldOfView = FovModifier.Fov;
            }
        }
    }
}
