using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Roguelike.Progression
{
    [Serializable]
    public sealed class MetaUpgradeEntry
    {
        public string id;
        public int level;
    }

    [Serializable]
    public sealed class MetaSaveData
    {
        public int version = 2;
        public int coins;
        public int totalCoinsEarned;
        public int runsPlayed;
        public int victories;
        public int bestFloor = 1;
        public int lastRunCoins;
        public int lastRunTotalCoins;
        public MetaUpgradeEntry[] upgrades = new MetaUpgradeEntry[0];
    }

    public readonly struct RunBankResult
    {
        public readonly int BankedCoins;
        public readonly int TotalCoins;

        public RunBankResult(int bankedCoins, int totalCoins)
        {
            BankedCoins = bankedCoins;
            TotalCoins = totalCoins;
        }
    }

    /// <summary>
    /// 跨局持久化的局外进度。
    ///
    /// 当前只保存金币与统计，但对外提供 AddCoins/TrySpendCoins，
    /// 后续技能树、角色、皮肤和武器解锁都可以直接复用同一份存档与消费接口。
    /// </summary>
    public static class MetaProgression
    {
        private const int CurrentVersion = 2;
        private const string SaveFileName = "meta_progression.json";

        private static MetaSaveData _data;
        private static bool _loaded;

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static int Coins { get { EnsureLoaded(); return _data.coins; } }
        public static int TotalCoinsEarned { get { EnsureLoaded(); return _data.totalCoinsEarned; } }
        public static int RunsPlayed { get { EnsureLoaded(); return _data.runsPlayed; } }
        public static int Victories { get { EnsureLoaded(); return _data.victories; } }
        public static int BestFloor { get { EnsureLoaded(); return _data.bestFloor; } }
        public static int LastRunCoins { get { EnsureLoaded(); return _data.lastRunCoins; } }
        public static int LastRunTotalCoins { get { EnsureLoaded(); return _data.lastRunTotalCoins; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Load();
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            _data = ReadFromDisk(SavePath);
            if (_data == null)
                _data = ReadFromDisk(SavePath + ".bak");
            if (_data == null)
                _data = new MetaSaveData();

            Normalize(_data);
            Debug.Log($"[Meta] 已加载局外进度：金币 {_data.coins}，局数 {_data.runsPlayed}。");
        }

        /// <summary>把本局剩余金币结算进局外金库；调用方负责保证每局只调用一次。</summary>
        public static RunBankResult BankRunCoins(int remainingCoins, int floor, bool victory)
        {
            EnsureLoaded();

            int banked = Mathf.Max(0, remainingCoins);
            _data.coins += banked;
            _data.totalCoinsEarned += banked;
            _data.runsPlayed++;
            if (victory) _data.victories++;
            _data.bestFloor = Mathf.Max(1, _data.bestFloor, floor);
            _data.lastRunCoins = banked;
            _data.lastRunTotalCoins = _data.coins;

            Save();
            Debug.Log($"[Meta] 带出 {banked} 金币，局外金库现有 {_data.coins}。");
            return new RunBankResult(banked, _data.coins);
        }

        public static int GetUpgradeLevel(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id) || _data.upgrades == null) return 0;

            for (int i = 0; i < _data.upgrades.Length; i++)
            {
                MetaUpgradeEntry entry = _data.upgrades[i];
                if (entry != null && entry.id == id)
                    return Mathf.Max(0, entry.level);
            }

            return 0;
        }

        public static bool HasUpgrade(string id)
        {
            MetaUpgradeDefinition definition = MetaUpgrades.Find(id);
            if (definition == null) return false;
            return GetUpgradeLevel(id) >= definition.maxLevel;
        }

        public static int GetUpgradeCost(MetaUpgradeDefinition definition)
        {
            return definition != null ? Mathf.Max(1, definition.cost) : 0;
        }

        public static bool CanPurchaseUpgrade(MetaUpgradeDefinition definition, out string reason)
        {
            EnsureLoaded();
            reason = null;
            if (definition == null)
            {
                reason = "无效节点";
                return false;
            }

            if (GetUpgradeLevel(definition.id) >= definition.maxLevel)
            {
                reason = "已激活";
                return false;
            }

            if (!string.IsNullOrEmpty(definition.prerequisiteId) &&
                GetUpgradeLevel(definition.prerequisiteId) <= 0)
            {
                reason = "需要先激活前置节点";
                return false;
            }

            int cost = GetUpgradeCost(definition);
            if (_data.coins < cost)
            {
                reason = $"金币不足（需要 {cost}）";
                return false;
            }

            return true;
        }

        /// <summary>激活一个技能树节点。成功后自动扣除金币并写入存档。</summary>
        public static bool TryPurchaseUpgrade(MetaUpgradeDefinition definition, out int spentCoins)
        {
            spentCoins = 0;
            if (!CanPurchaseUpgrade(definition, out _)) return false;

            int cost = GetUpgradeCost(definition);
            _data.coins -= cost;
            SetUpgradeLevel(definition.id, GetUpgradeLevel(definition.id) + 1);
            Save();
            spentCoins = cost;
            return true;
        }

        /// <summary>后续技能树、角色、皮肤与武器解锁使用。</summary>
        public static bool TrySpendCoins(int amount)
        {
            EnsureLoaded();
            if (amount <= 0 || _data.coins < amount) return false;

            _data.coins -= amount;
            Save();
            return true;
        }

        public static void AddCoins(int amount)
        {
            EnsureLoaded();
            if (amount <= 0) return;

            _data.coins += amount;
            _data.totalCoinsEarned += amount;
            Save();
        }

        /// <summary>开发调试用：清空全部局外进度。</summary>
        public static void ResetProgress()
        {
            _data = new MetaSaveData();
            _loaded = true;
            Save();
            Debug.Log("[Meta] 已清空局外进度。");
        }

        public static void ReloadFromDisk()
        {
            _loaded = false;
            _data = null;
            Load();
        }

        private static void EnsureLoaded()
        {
            if (!_loaded) Load();
        }

        private static MetaSaveData ReadFromDisk(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;

                MetaSaveData data = JsonUtility.FromJson<MetaSaveData>(json);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Meta] 读取存档失败：{path}\n{exception.Message}");
                return null;
            }
        }

        private static void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(_data, true);
                string tempPath = SavePath + ".tmp";

                File.WriteAllText(tempPath, json);
                if (File.Exists(SavePath))
                    File.Copy(SavePath, SavePath + ".bak", true);
                File.Copy(tempPath, SavePath, true);
                File.Delete(tempPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Meta] 写入存档失败：{SavePath}\n{exception.Message}");
            }
        }

        private static void Normalize(MetaSaveData data)
        {
            if (data.version < CurrentVersion) data.version = CurrentVersion;
            data.coins = Mathf.Max(0, data.coins);
            data.totalCoinsEarned = Mathf.Max(0, data.totalCoinsEarned);
            data.runsPlayed = Mathf.Max(0, data.runsPlayed);
            data.victories = Mathf.Max(0, data.victories);
            data.bestFloor = Mathf.Max(1, data.bestFloor);
            data.lastRunCoins = Mathf.Max(0, data.lastRunCoins);
            data.lastRunTotalCoins = Mathf.Max(0, data.lastRunTotalCoins);

            if (data.upgrades == null)
                data.upgrades = new MetaUpgradeEntry[0];

            for (int i = 0; i < data.upgrades.Length; i++)
            {
                MetaUpgradeEntry entry = data.upgrades[i];
                if (entry == null) continue;
                entry.level = Mathf.Max(0, entry.level);
            }
        }

        private static void SetUpgradeLevel(string id, int level)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (_data.upgrades == null)
                _data.upgrades = new MetaUpgradeEntry[0];

            for (int i = 0; i < _data.upgrades.Length; i++)
            {
                MetaUpgradeEntry entry = _data.upgrades[i];
                if (entry == null || entry.id != id) continue;
                entry.level = Mathf.Max(0, level);
                return;
            }

            var entries = new List<MetaUpgradeEntry>(_data.upgrades)
            {
                new MetaUpgradeEntry { id = id, level = Mathf.Max(0, level) }
            };
            _data.upgrades = entries.ToArray();
        }
    }
}
