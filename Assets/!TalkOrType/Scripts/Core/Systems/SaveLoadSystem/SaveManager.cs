using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RKS.TalkOrType.Core.Managers
{
    public class SaveManager : RKSBehaviour
    {
        [SerializeField] private string fileName = "RKS_Data.rkst";

        private string filePath;
        private string backupPath;
        private string tempPath;

        private readonly char[] rechAlphabet = { 'R', 'E', 'C', 'H' };

        public GameData.Data CurrentData { get; private set; }

        private bool _dirty;

        protected override void OnInjected()
        {
            filePath = Path.Combine(Application.persistentDataPath, fileName);
            backupPath = filePath + ".bak";
            tempPath = filePath + ".tmp";

            CurrentData = LoadInternal();
        }

        public void Write(GameData.Data data)
        {
            CurrentData = data ?? new GameData.Data();
            _dirty = true;
            SaveIfNeeded();
        }

        public void Write()
        {
            CurrentData ??= new GameData.Data();
            _dirty = true;
            SaveIfNeeded();
        }

        public GameData.Data Load()
        {
            CurrentData = LoadInternal();
            return CurrentData;
        }

        public void ResetToDefault()
        {
            CurrentData = new GameData.Data();
            _dirty = true;
            SaveIfNeeded();
        }

        void SaveIfNeeded()
        {
            if (!_dirty) return;

            WriteToFile(CurrentData);
            _dirty = false;
        }

        void WriteToFile(GameData.Data data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string rech = Encode(json);

                if (File.Exists(filePath))
                    File.Copy(filePath, backupPath, true);

                File.WriteAllText(tempPath, rech);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Move(tempPath, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
            }
        }

        GameData.Data LoadInternal()
        {
            if (!File.Exists(filePath))
                return CreateDefault();

            try
            {
                string rech = File.ReadAllText(filePath);
                string json = Decode(rech);

                var data = JsonUtility.FromJson<GameData.Data>(json);

                if (data == null)
                    return CreateDefault();

                if (data.version != GameData.VERSION)
                {
                    Debug.LogWarning("[SaveManager] Version mismatch → reset to default");

                    return CreateDefault();
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Load failed → backup: {ex.Message}");

                return LoadBackupOrDefault();
            }
        }

        GameData.Data LoadBackupOrDefault()
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    string json = Decode(File.ReadAllText(backupPath));
                    var data = JsonUtility.FromJson<GameData.Data>(json);

                    if (data != null && data.version == GameData.VERSION)
                        return data;
                }
            }
            catch { }

            return CreateDefault();
        }

        GameData.Data CreateDefault()
        {
            var def = new GameData.Data();
            WriteToFile(def);
            return def;
        }

        string Encode(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            StringBuilder sb = new(bytes.Length * 4);

            foreach (byte b in bytes)
            {
                sb.Append(rechAlphabet[(b >> 6) & 3]);
                sb.Append(rechAlphabet[(b >> 4) & 3]);
                sb.Append(rechAlphabet[(b >> 2) & 3]);
                sb.Append(rechAlphabet[b & 3]);
            }

            return sb.ToString();
        }

        string Decode(string rech)
        {
            byte[] bytes = new byte[rech.Length / 4];

            for (int i = 0; i < rech.Length; i += 4)
            {
                bytes[i / 4] = (byte)(
                    (Map(rech[i]) << 6) |
                    (Map(rech[i + 1]) << 4) |
                    (Map(rech[i + 2]) << 2) |
                    Map(rech[i + 3])
                );
            }

            return Encoding.UTF8.GetString(bytes);
        }

        int Map(char c)
        {
            return c switch
            {
                'R' => 0,
                'E' => 1,
                'C' => 2,
                'H' => 3,
                _ => throw new Exception("Invalid RECH char")
            };
        }
    }
}