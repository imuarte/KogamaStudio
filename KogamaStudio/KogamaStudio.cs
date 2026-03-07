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

        public override void OnUpdate()
        {

            string cursor = Cursor.visible || Cursor.lockState == CursorLockMode.None ? "true" : "false";
            PipeClient.SendCommand($"cursor|{cursor}");

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
                }

            }

            if (gameInitialized)
            {
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

            // recovery mode
            //if (UnityEngine.Input.GetKeyDown(KeyCode.F8))
            //{
            //    RecoveryMode.EnableRecoveryMode();
            //}

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

            if (FovModifier.Enabled)
                mgr.FieldOfView = FovModifier.Fov;
        }
    }
}
