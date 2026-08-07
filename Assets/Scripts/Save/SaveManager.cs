using System.IO;
using UnityEngine;

namespace SteelTempest.Save
{
    /// <summary>
    /// Persists and loads <see cref="SaveData"/> to disk as JSON.
    /// Registered in the DI container at bootstrap and saved on pause.
    /// </summary>
    public sealed class SaveManager
    {
        private const string FileName = "savegame.json";

        public SaveData Data { get; private set; }

        private string _path;
        private string Path => _path ??= System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public void Initialize()
        {
            if (File.Exists(Path))
            {
                try
                {
                    Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path)) ?? new SaveData();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SteelTempest] Failed to load save: {e.Message}");
                    Data = new SaveData();
                }
            }
            else
            {
                Data = new SaveData();
            }
        }

        public void Save()
        {
            if (Data == null) return;
            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(Data, true));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteelTempest] Failed to write save: {e.Message}");
            }
        }

        /// <summary>Loads state from a JSON string (tests, cloud saves).</summary>
        public void LoadFromString(string json)
        {
            try
            {
                Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteelTempest] Failed to parse save JSON: {e.Message}");
                Data = new SaveData();
            }
        }

        /// <summary>Serializes the current state to a JSON string (tests, cloud saves).</summary>
        public string ToJson() => JsonUtility.ToJson(Data, true);

        public void DeleteSave()
        {
            if (File.Exists(Path)) File.Delete(Path);
        }
    }
}