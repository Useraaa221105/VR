using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Podz
{
    [Serializable] public class ItemCount { public ItemId id; public int n; }

    [Serializable]
    public class SaveData
    {
        public int coins = 0;
        public int day = 1;
        public bool hasCleaver, hasHammer, hasShield;
        public bool dragonDefeated, finished;
        public List<string> seenMonsters = new List<string>();
        public List<ItemCount> fridge = new List<ItemCount>();
        public bool kitchenTutorialDone;

        public int Stage => 1 + (hasCleaver ? 1 : 0) + (hasHammer ? 1 : 0);
    }

    public static class SaveSystem
    {
        static string PathFile => Path.Combine(Application.persistentDataPath, "podz_save.json");

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(PathFile))
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFile));
                    if (d != null) return d;
                }
            }
            catch (Exception e) { Debug.LogWarning("[Save] не удалось прочитать: " + e.Message); }
            return new SaveData();
        }

        public static void Save(SaveData d, ItemStore fridge)
        {
            d.fridge.Clear();
            foreach (var kv in fridge.All) d.fridge.Add(new ItemCount { id = kv.Key, n = kv.Value });
            try
            {
                File.WriteAllText(PathFile, JsonUtility.ToJson(d, true));
                Debug.Log("[Save] сохранено: " + PathFile);
            }
            catch (Exception e) { Debug.LogWarning("[Save] ошибка: " + e.Message); }
        }

        public static void Delete()
        {
            if (File.Exists(PathFile)) File.Delete(PathFile);
        }
    }
}
