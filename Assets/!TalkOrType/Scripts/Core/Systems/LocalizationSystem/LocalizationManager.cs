using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RKS.TalkOrType.Core.Managers
{
    public class LocalizationManager : RKSBehaviour
    {
        public string currentLanguage { get; private set; }

        private readonly Dictionary<string, string> _current = new();
        private readonly Dictionary<string, string> _fallback = new();

        public event Action OnLanguageChanged;

        const string FALLBACK_LANG = "en_US";

        private bool _isLoading;    


        protected override void OnReady()
        {
            LoadInitialLanguage();
        }

        void LoadInitialLanguage()
        {
            var lang = string.IsNullOrEmpty(Save.CurrentData.language)
                ? GetSystemLanguage()
                : Save.CurrentData.language;

            SetLanguage(lang);
        }

        string GetSystemLanguage()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Russian => "ru_RU",
                SystemLanguage.German => "de_DE",
                SystemLanguage.Spanish => "es_ES",
                _ => FALLBACK_LANG
            };
        }
        

        public async void SetLanguage(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
                return;

            if (lang == currentLanguage)
                return;

            if (_isLoading)
                return;

            _isLoading = true;

            currentLanguage = lang;

            Save.CurrentData.language = lang;
            Save.Write();

            _current.Clear();
            _fallback.Clear();

            try
            {
                await LoadLanguage(lang, _current);

                if (lang != FALLBACK_LANG)
                    await LoadLanguage(FALLBACK_LANG, _fallback);

                OnLanguageChanged?.Invoke();
                Debug.Log($"[LocalizationManager] I'm ready! Language: {currentLanguage}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationManager] I ran into an error here: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        async Task LoadLanguage(string lang, Dictionary<string, string> target)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Languages", lang + ".json");

            string json;

            if (Application.platform == RuntimePlatform.Android)
            {
                using var www = UnityWebRequest.Get(path);
                var op = www.SendWebRequest();

                while (!op.isDone)
                    await Task.Yield();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[LocalizationManager] Failed load: {lang}");
                    return;
                }

                json = www.downloadHandler.text;
            }
            else
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"[LocalizationManager] I can't find the translation file at this path: {path}. I'm continuing my work.");
                    return;
                }

                json = File.ReadAllText(path);
            }

            var data = JsonUtility.FromJson<LocalizationData>(json);

            if (data?.items == null)
                return;

            foreach (var item in data.items)
            {
                target[item.key] = item.value;
            }
        }

        public string Get(string key, params object[] args)
        {
            if (_current.TryGetValue(key, out var value))
                return Format(value, args);

            if (_fallback.TryGetValue(key, out var fallback))
                return Format(fallback, args);

            Debug.LogWarning($"[LocalizationManager] Missing key: {key}");
            return key;
        }

        string Format(string value, object[] args)
        {
            if (args == null || args.Length == 0)
                return value;

            try
            {
                return string.Format(value, args);
            }
            catch
            {
                return value;
            }
        }
    }
}