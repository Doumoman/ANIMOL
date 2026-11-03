using System;
using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>Small local preview save. No campaign service, inventory, reward or network writes.</summary>
    public static class StorySaveStore
    {
        public static string Key(string packageId) { return "WilliamParadox.Story.V14." + packageId; }
        public static StorySaveData Load(string packageId)
        {
            try
            {
                var json = PlayerPrefs.GetString(Key(packageId), "");
                return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<StorySaveData>(json);
            }
            catch (ArgumentException) { return null; }
        }
        public static void Save(StorySaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.packageId)) return;
            PlayerPrefs.SetString(Key(data.packageId), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
        public static void Delete(string packageId) { PlayerPrefs.DeleteKey(Key(packageId)); PlayerPrefs.Save(); }
    }
}
