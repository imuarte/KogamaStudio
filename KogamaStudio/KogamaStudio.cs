using Il2Cpp;
using KogamaStudio.Camera;
using KogamaStudio.Tools;
using KogamaStudio.Translator;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

[assembly: MelonInfo(typeof(KogamaStudio.KogamaStudio), "KogamaStudio", "0.4.2", "Amuarte")]
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
                    TextCommand.NotifyUser("<b>KogamaStudio</b> v0.4.2 loaded!\nPress <b>F2</b> to open menu.");

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
    }
}
