using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Il2CppSystem;


namespace KogamaStudio.Translator
{
    [HarmonyPatch]
    internal class AddLinePatch
    {
        public static SendMessageControl SendMessageControlInstance = null;

        public static bool TranslateOwnMessagesEnabled = false;
        public static string TranslateOwnMessagesLanguage = "en";

        public static bool TranslateOtherMessagesEnabled = false;
        public static string TranslateOtherMessagesLanguage = "en";

        public static bool TranslateTextCubesEnabled = false;
        public static string TranslateTextCubesLanguage = "en";

        public enum MessageTranslationType
        {
            None,
            OwnMessages,
            OthersMessages
        }
        public static MessageTranslationType CurrentTranslationMode = MessageTranslationType.None;

            [HarmonyPatch(typeof(ChatControllerBase), "AddChatLine")]
        [HarmonyPrefix]
        private static void AddChatLinePrefix(Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object> data)
        {
            
        }

        [HarmonyPatch(typeof(ChatControllerBase), "FormatTeamChatMessage")]
        [HarmonyPostfix]
        private static void SayChatPostfix(ref string __result)
        {
            
        }

        [HarmonyPatch(typeof(ChatControllerBase), "FormatTeamChatMessage")]
        [HarmonyPostfix]
        private static void TeamChatPostfix(ref string __result)
        {
            
        }

        [HarmonyPatch(typeof(SendMessageControl), "SendChatMessage")]
        [HarmonyPrefix]
        private static bool SendMessagePrefix(SendMessageControl __instance, ref string chatMsg)
        {
            var translator = MessageTranslator.Instance;

            if (!TranslateOwnMessagesEnabled) return true;

            // If translation is ready, send the translated message
            if (translator.TranslationReady)
            {
                translator.TranslationReady = false;
                return true;
            }

            // Otherwise block send and translate in background
            SendMessageControlInstance = __instance;
            translator.Translate(chatMsg, TranslateOwnMessagesLanguage);
            CurrentTranslationMode = MessageTranslationType.OwnMessages;
            chatMsg = "";
            return true;
        }

    }
}
