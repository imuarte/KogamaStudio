using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using Il2CppSystem.Linq;


namespace KogamaStudio.Translator
{
    internal class MessageTranslator
    {
        private static readonly MessageTranslator _instance = new MessageTranslator();
        public static MessageTranslator Instance => _instance;

        private HttpClient client = new HttpClient();
        private string _targetLanguage = "en";
        private string _lastTranslation = "";
        private bool _translationReady = false;

        public Action<string[]> OnChunkTranslated;

        public MessageTranslator()
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        }

        public string TargetLanguage
        {
            get => _targetLanguage;
            set => _targetLanguage = value;
        }
        public string LastTranslation
        {
            get => _lastTranslation;
            private set => _lastTranslation = value;
        }

        public bool TranslationReady
        {
            get => _translationReady;
            set => _translationReady = value;
        }

        public void TranslateArray(string[] texts, string targetLang = null)
        {
            targetLang ??= _targetLanguage;
            TranslationReady = false;
            var thread = new Thread(() => TranslateArrayAsync(texts, targetLang));
            thread.IsBackground = true;
            thread.Start();
        }

        private void TranslateArrayAsync(string[] texts, string targetLang)
        {
            const int maxCharsPerRequest = 100;
            var chunks = new List<string[]>();
            var currentChunk = new List<string>();
            int currentCharCount = 0;

            foreach (var text in texts)
            {
                if (currentCharCount + text.Length > maxCharsPerRequest && currentChunk.Count > 0)
                {
                    chunks.Add(currentChunk.ToArray());
                    currentChunk = new List<string>();
                    currentCharCount = 0;
                }

                currentChunk.Add(text);
                currentCharCount += text.Length;
            }

            if (currentChunk.Count > 0)
                chunks.Add(currentChunk.ToArray());

            var allTranslations = new List<string>();

            foreach (var chunk in chunks)
            {
                var chunkTranslations = TranslateChunk(chunk, targetLang);

                if (chunkTranslations == null)
                {
                    LastTranslation = "[]";
                    TranslationReady = true;
                    return;
                }

                allTranslations.AddRange(chunkTranslations);
                OnChunkTranslated?.Invoke(chunkTranslations);
                System.Threading.Thread.Sleep(500);
            }

            LastTranslation = JsonConvert.SerializeObject(allTranslations);
            TranslationReady = true;
        }

        private string[] TranslateChunk(string[] chunk, string targetLang)
        {
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var payload = new { texts = chunk, targetLanguage = targetLang };
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var res = client.PostAsync("https://kogamastudio.onrender.com/translate/translate", content).Result;

                    dynamic data = JsonConvert.DeserializeObject(res.Content.ReadAsStringAsync().Result);
                    var translationsJson = data.translations.ToString();
                    return JsonConvert.DeserializeObject<string[]>(translationsJson);
                }
                catch (Exception ex)
                {
                    KogamaStudio.Log.LogWarning($"Chunk translation error: {ex.Message}");
                    retryCount++;
                    if (retryCount < maxRetries)
                        System.Threading.Thread.Sleep(1000);
                }
            }

            return null;
        }

        public void Translate(string text, string targetLang = null)
        {
            targetLang ??= _targetLanguage;
            TranslationReady = false;

            var thread = new Thread(() => TranslateAsync(text, targetLang));
            thread.IsBackground = true;
            thread.Start();
        }

        private void TranslateAsync(string text, string targetLang)
        {
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var payload = new { texts = new[] { text }, targetLanguage = targetLang };
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var res = client.PostAsync("https://kogamastudio.onrender.com/translate/translate", content).Result;

                    dynamic data = JsonConvert.DeserializeObject(res.Content.ReadAsStringAsync().Result);
                    var translationsJson = data.translations.ToString();
                    var translations = JsonConvert.DeserializeObject<string[]>(translationsJson);
                    LastTranslation = translations[0];
                    TranslationReady = true;
                    return;
                }
                catch (Exception ex)
                {
                    KogamaStudio.Log.LogWarning($"Translation error: {ex.Message}");
                    retryCount++;
                    if (retryCount < maxRetries)
                        System.Threading.Thread.Sleep(1000);
                }
            }
        }
    }
}