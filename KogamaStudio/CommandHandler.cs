using Harmony;
using Il2Cpp;
using Il2CppAssets.Scripts.WorldObjectTypes.MVTextMsg;
using Il2CppBorodar.FarlandSkies.CloudyCrownPro.DotParams;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Runtime.InteropServices;
using KogamaStudio.Generating.Models;
using KogamaStudio.Objects;
using KogamaStudio.ResourcePacks.Materials;
using KogamaStudio.Tools;
using KogamaStudio.Translator;
using MelonLoader;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KogamaStudio
{
    public class CommandHandler
    {
        private static System.Collections.Generic.Dictionary<int, string> _originalTexts = new System.Collections.Generic.Dictionary<int, string>();

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

        private static string HandleGetText(int id)
        {
            var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);
            if (wo == null) return null;

            var data = wo.GetType().GetProperty("Data")?.GetValue(wo) as Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object>;
            if (data == null) return null;

            foreach (var kvp in data)
            {
                var valuePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(kvp.Value);
                return Il2CppInterop.Runtime.IL2CPP.Il2CppStringToManaged(valuePtr);
            }

            return null;
        }

        private static void HandleSetText(int id, string value)
        {
            var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);
            if (wo == null) return;

            var textMsg = wo.Cast<MVTextMsg>();
            if (textMsg?.msgObject == null) return;

            var textMesh = textMsg.msgObject.GetType().GetProperty("TextMesh")?.GetValue(textMsg.msgObject);
            if (textMesh == null) return;

            textMesh.GetType().GetProperty("text")?.SetValue(textMesh, value);
        }

        private static void HandleBackupTextCubes()
        {
            _originalTexts.Clear();

            Il2CppSystem.Collections.Generic.HashSet<int> ids =
                new Il2CppSystem.Collections.Generic.HashSet<int>();
            MVGameControllerBase.WOCM.GetAllWoIds(75578, ids);

            foreach (int id in ids)
            {
                var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);
                if (wo != null && $"{wo.type}".Contains("TextMsg"))
                {
                    var text = HandleGetText(id);
                    if (!string.IsNullOrEmpty(text))
                    {
                        _originalTexts[id] = text;
                    }
                }
            }
        }

        private static void HandleRestoreTextCubes()
        {
            foreach (var kvp in _originalTexts)
            {
                HandleSetText(kvp.Key, kvp.Value);
            }
        }

        private static System.Collections.Generic.Queue<(int id, string text)> _translateQueue =
    new System.Collections.Generic.Queue<(int id, string text)>();

        private static void HandleTranslateTextCubes(string targetLanguage)
        {
            if (_originalTexts.Count == 0) return;

            var textsArray = _originalTexts.Values.ToArray();

            foreach (var kvp in _originalTexts)
                HandleSetText(kvp.Key, "Translating...");

            MelonCoroutines.Start(TranslateAllAtOnce(textsArray, targetLanguage));
        }


        private static System.Collections.IEnumerator TranslateAllAtOnce(string[] textsArray, string targetLanguage)
        {
            MessageTranslator.Instance.TranslateArray(textsArray, targetLanguage);

            int wait = 0;
            while (!MessageTranslator.Instance.TranslationReady && wait < 100)
            {
                yield return new WaitForSeconds(0.1f);
                wait++;
            }

            if (MessageTranslator.Instance.TranslationReady)
            {
                var translations = JsonConvert.DeserializeObject<string[]>(MessageTranslator.Instance.LastTranslation);
                int index = 0;

                foreach (var kvp in _originalTexts)
                {
                    if (index < translations.Length)
                        HandleSetText(kvp.Key, translations[index].Trim());
                    index++;
                }

                TextCommand.NotifyUser("Translation complete");
            }
        }

        private static System.Collections.IEnumerator TranslateAndSetAsync(int id, string text, string lang)
        {
            MessageTranslator.Instance.Translate(text, lang);

            int wait = 0;
            while (!MessageTranslator.Instance.TranslationReady && wait < 50)
            {
                yield return new WaitForSeconds(0.1f);
                wait++;
            }

            if (MessageTranslator.Instance.TranslationReady)
            {
                HandleSetText(id, MessageTranslator.Instance.LastTranslation);
                MessageTranslator.Instance.TranslationReady = false;
            }
        }

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
            string[] parts = cmd.Split('|');
            string command = parts[0];

            MelonCoroutines.Start(ExecuteCommand(command, parts.Length > 1 ? parts[1] : ""));
        }

        public static CursorLockMode previousLockState = CursorLockMode.None;
        public static int targetWoId = -1;
        public static string targetPlayerName = null;
        public static bool previousVisible = true;
        public static MVWorldObjectClient savedWo = null;


        private static System.Collections.IEnumerator ExecuteCommand(string command, string param)
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
                    case "test":
                        HandleSetText(targetWoId, "hello world!");

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
                    case "translate_text_cubes_translate":
                        HandleBackupTextCubes();
                        HandleTranslateTextCubes(AddLinePatch.TranslateTextCubesLanguage);
                        break;
                    case "translate_text_cubes_language":
                        AddLinePatch.TranslateTextCubesLanguage = param;
                        break;
                    case "translate_text_cubes_enabled":
                        if (param == "true")
                        {
                            AddLinePatch.TranslateOtherMessagesEnabled = true;
                            HandleBackupTextCubes();
                        }
                        else
                        {
                            AddLinePatch.TranslateOtherMessagesEnabled = false;
                            HandleRestoreTextCubes();
                        }

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
}
