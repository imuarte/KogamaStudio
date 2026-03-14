using Il2Cpp;
using KogamaStudio.Camera;
using KogamaStudio.Clipboard;
using KogamaStudio.Tools;
using KogamaStudio.Tools.Properties;
using KogamaStudio.Translator;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

[assembly: MelonInfo(typeof(KogamaStudio.KogamaStudio), "KogamaStudio", KogamaStudio.KSVersion.Value, "Amuarte")]
[assembly: MelonGame("Multiverse ApS", "KoGaMa")]

namespace KogamaStudio
{
    public class KogamaStudio : MelonMod
    {
public static bool gameInitialized = false;
        private static bool harmonyPatched = false;
        public override void OnInitializeMelon()
        {

            if (harmonyPatched) return;

            DllLoader.Load("KogamaStudio-ImGui-Hook.dll");
            CommandHandler.StartListening();
            DirectoryManager.Initialize();

            var methods = typeof(SelectionController).GetMethods()
    .Where(m => m.Name == "SelectWO");
            foreach (var m in methods)
                MelonLogger.Msg($"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
        }

        private bool? _lastFullscreen = null;
        private static bool _wasPasting = false;
        private static System.Collections.Concurrent.ConcurrentQueue<System.Action> _mainThreadQueue = new();
        private static int _lastSentPlaced = -1;
        private static int _lastObjectCount = -1;
        private static float _explorerRefreshAt = -1f;

        private static bool _lastCursorState = false;

        public static void RunOnMainThread(System.Action action) => _mainThreadQueue.Enqueue(action);

        public override void OnUpdate()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
                action();

            bool cursorState = Cursor.visible || Cursor.lockState == CursorLockMode.None;
            if (cursorState != _lastCursorState)
            {
                _lastCursorState = cursorState;
                PipeClient.SendCommand($"cursor|{(cursorState ? "true" : "false")}");
            }

            bool fs = Screen.fullScreen;
            if (fs != _lastFullscreen)
            {
                _lastFullscreen = fs;
                PipeClient.SendCommand($"fullscreen|{(fs ? "true" : "false")}");
            }

            if (!gameInitialized)
            {
                if (MVGameControllerBase.IsInitialized)
                {
                    MelonLogger.Msg("KogamaStudio loaded!");


                    gameInitialized = true;
                    PipeClient.SendCommand("game_initialized");
                    TextCommand.NotifyUser($"<b>KogamaStudio</b> v{KSVersion.Value} loaded!\nPress <b>F2</b> to open menu.");
                    Players.PlayerList.Start();
                    GlLineDrawer.Init();
                    GameInfo.Init();
                    PipeClient.SendCommand("inventory_clear");
                    CommandHandler.RefreshInventory();
                    Explorer.ExplorerManager.RefreshWhenReady();
                }

            }

            if (gameInitialized)
            {
                var wocm = MVGameControllerBase.WOCM;
                if (wocm?.worldObjects != null)
                {
                    int count = wocm.worldObjects.Count;
                    if (count != _lastObjectCount)
                    {
                        _lastObjectCount = count;
                        _explorerRefreshAt = Time.time + 0.15f;
                    }
                    if (_explorerRefreshAt > 0f && Time.time >= _explorerRefreshAt)
                    {
                        _explorerRefreshAt = -1f;
                        Explorer.ExplorerManager.Refresh();
                    }
                }

                bool pasting = KogamaModFramework.Operations.CubeOperations.IsBuilding;
                if (pasting)
                {
                    float progress = KogamaModFramework.Operations.CubeOperations.Progress;
                    int total = ClipboardManager.LastPasteTotalCubes;
                    int placed = (int)(progress * total);
                    if (placed != _lastSentPlaced)
                    {
                        _lastSentPlaced = placed;
                        int batchSize = KogamaModFramework.Operations.CubeOperations.BatchSize;
                        float timePerBatch = KogamaModFramework.Operations.CubeOperations.FrameDelay / 60f;
                        int remainingBatches = (total - placed + batchSize - 1) / batchSize;
                        float timeLeft = remainingBatches * timePerBatch;
                        PipeClient.SendCommand($"clipboard_paste_progress|{placed}|{total}|{timeLeft:F1}");
                    }
                }
                else if (_wasPasting)
                {
                    _lastSentPlaced = -1;
                    PipeClient.SendCommand("clipboard_paste_done");
                }
                _wasPasting = pasting;

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

        public override void OnLateUpdate()
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
