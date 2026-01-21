using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using Il2CppSystem.Linq;
using MelonLoader;

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

        public MessageTranslator()
        {
            client.Timeout = TimeSpan.FromSeconds(60);
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
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var payload = new { texts = texts, targetLanguage = targetLang };
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var res = client.PostAsync("https://kogamastudio.onrender.com/translate/translate", content).Result;

                    dynamic data = JsonConvert.DeserializeObject(res.Content.ReadAsStringAsync().Result);
                    LastTranslation = data.translations.ToString();
                    TranslationReady = true;
                    return;
                }
                catch
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                        System.Threading.Thread.Sleep(1000);
                }
            }
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
                catch
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                        System.Threading.Thread.Sleep(1000);
                }
            }
        }
    }
}