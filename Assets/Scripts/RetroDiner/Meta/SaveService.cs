using System;
using System.IO;
using UnityEngine;

namespace RetroDiner.Meta
{
    /// <summary>JSON save at persistentDataPath. PlayerPrefs is used only for the save-format version key.</summary>
    public class SaveService
    {
        const string VersionKey = "rd3d_save_version";
        const int CurrentVersion = 1;

        public string FilePath => Path.Combine(Application.persistentDataPath, "retrodiner_profile.json");

        public PlayerProfile Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var p = JsonUtility.FromJson<PlayerProfile>(json);
                    if (p != null)
                    {
                        if (p.stars == null || p.stars.Length < 64) Array.Resize(ref p.stars, 64);
                        return p;
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning("[Save] Load failed, starting fresh: " + e.Message); }
            var fresh = new PlayerProfile();
            PlayerPrefs.SetInt(VersionKey, CurrentVersion);
            return fresh;
        }

        public void Save(PlayerProfile profile)
        {
            try
            {
                profile.saveVersion = CurrentVersion;
                File.WriteAllText(FilePath, JsonUtility.ToJson(profile, true));
                PlayerPrefs.SetInt(VersionKey, CurrentVersion);
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("[Save] Save failed: " + e.Message); }
        }

        public void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
