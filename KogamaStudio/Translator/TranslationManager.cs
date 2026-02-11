using Il2Cpp;
using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KogamaStudio.Translator;

internal class TranslationManager
{
    private static Dictionary<int, string> _originalTexts = new();
    private static HashSet<int> _translatedIds = new();
    private static bool _isTranslating = false;

    internal static void BackupTextCubes()
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
                var text = GetText(id);
                if (!string.IsNullOrEmpty(text))
                {
                    _originalTexts[id] = text;
                }
            }
        }
    }

    internal static void TranslateTextCubes(string targetLanguage)
    {
        if (_originalTexts.Count == 0) return;

        int playerWoId = MVGameControllerBase.LocalPlayer.WoId;
        var playerWo = MVGameControllerBase.WOCM?.GetWorldObjectClient(playerWoId);
        Vector3 playerPos = playerWo?.Position ?? Vector3.zero;

        var sortedTexts = _originalTexts.OrderBy(kvp =>
        {
            var textWo = MVGameControllerBase.WOCM?.GetWorldObjectClient(kvp.Key);
            float distance = Vector3.Distance(playerPos, textWo?.Position ?? Vector3.zero);
            return distance;
        }).ToList();

        var textsArray = sortedTexts.Select(x => x.Value).ToArray();
        var textIds = new System.Collections.Generic.List<int>(sortedTexts.Select(x => x.Key));

        foreach (var id in textIds)
            SetText(id, "Translating...");

        MelonCoroutines.Start(TranslateAllAtOnce(textsArray, textIds, targetLanguage));
    }

    internal static System.Collections.IEnumerator TranslateAllAtOnce(string[] textsArray, System.Collections.Generic.List<int> textIds, string targetLanguage)
    {
        int chunkIndex = 0;

        MessageTranslator.Instance.OnChunkTranslated = (translations) =>
        {
            for (int i = 0; i < translations.Length && chunkIndex < textIds.Count; i++)
            {
                SetText(textIds[chunkIndex], translations[i].Trim());
                chunkIndex++;
            }
        };

        MessageTranslator.Instance.TranslateArray(textsArray, targetLanguage);

        int wait = 0;
        while (!MessageTranslator.Instance.TranslationReady && wait < 500)
        {
            yield return new WaitForSeconds(0.1f);
            wait++;
        }

        TextCommand.NotifyUser("Translation complete");
    }

    internal static void UpdateLiveTranslation()
    {
        if (_isTranslating) return;

        int playerWoId = MVGameControllerBase.LocalPlayer.WoId;
        var playerWo = MVGameControllerBase.WOCM?.GetWorldObjectClient(playerWoId);
        Vector3 playerPos = playerWo?.Position ?? Vector3.zero;

        var nearbyTexts = _originalTexts.Where(kvp => !_translatedIds.Contains(kvp.Key))
            .OrderBy(kvp =>
            {
                var textWo = MVGameControllerBase.WOCM?.GetWorldObjectClient(kvp.Key);
                return Vector3.Distance(playerPos, textWo?.Position ?? Vector3.zero);
            }).Take(10).ToList();

        if (nearbyTexts.Count == 0)
            return;

        var textsArray = nearbyTexts.Select(x => x.Value).ToArray();
        var textIds = new System.Collections.Generic.List<int>(nearbyTexts.Select(x => x.Key));

        foreach (var id in textIds)
            SetText(id, "Translating...");

        _isTranslating = true;
        MelonCoroutines.Start(TranslateAndContinue(textsArray, textIds, AddLinePatch.TranslateTextCubesLanguage));

        foreach (var id in textIds)
            _translatedIds.Add(id);
    }

    internal static System.Collections.IEnumerator TranslateAndContinue(string[] textsArray, System.Collections.Generic.List<int> textIds, string targetLanguage)
    {
        int chunkIndex = 0;

        MessageTranslator.Instance.OnChunkTranslated = (translations) =>
        {
            for (int i = 0; i < translations.Length && chunkIndex < textIds.Count; i++)
            {
                SetText(textIds[chunkIndex], translations[i].Trim());
                chunkIndex++;
            }
        };

        MessageTranslator.Instance.TranslateArray(textsArray, targetLanguage);

        int wait = 0;
        while (!MessageTranslator.Instance.TranslationReady && wait < 500)
        {
            yield return new WaitForSeconds(0.1f);
            wait++;
        }

        _isTranslating = false;
    }

    internal static System.Collections.IEnumerator StartLiveTranslation()
    {
        while (AddLinePatch.TranslateTextCubesEnabled)
        {
            UpdateLiveTranslation();
            yield return new WaitForSeconds(0.1f);
        }
    }

    internal static void RestoreTextCubes()
    {
        foreach (var kvp in _originalTexts)
        {
            SetText(kvp.Key, kvp.Value);
        }

        _translatedIds.Clear();
    }

    internal static string GetText(int id)
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

    internal static void SetText(int id, string value)
    {
        var wo = MVGameControllerBase.WOCM?.GetWorldObjectClient(id);
        if (wo == null) return;

        var textMsg = wo.Cast<MVTextMsg>();
        if (textMsg?.msgObject == null) return;

        var textMesh = textMsg.msgObject.GetType().GetProperty("TextMesh")?.GetValue(textMsg.msgObject);
        if (textMesh == null) return;

        var textProperty = textMesh.GetType().GetProperty("text");
        if (textProperty != null && textProperty.CanWrite)
            textProperty.SetValue(textMesh, value);
    }
}
