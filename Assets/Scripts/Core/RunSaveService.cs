using System;
using System.IO;
using UnityEngine;

namespace Roguelike.Core
{
    /// <summary>单个房间在存档中的状态。</summary>
    [Serializable]
    public sealed class RunRoomSave
    {
        public int x;
        public int y;
        public bool visited;
        public bool cleared;
        public bool rewardClaimed;
        public bool secretUsed;
    }

    /// <summary>
    /// 单局进度快照。保存到 persistentDataPath，可在退出游戏后继续。
    /// 地牢本身按 seed 重建，这里只记录无法重新推导的运行时状态。
    /// </summary>
    [Serializable]
    public sealed class RunSaveData
    {
        public int version = RunSaveService.CurrentVersion;
        public int floor = 1;
        public int seed;
        public int coins;
        public int health;
        public int shield;
        public int currentRoomX;
        public int currentRoomY;
        public bool bossCleared;
        public bool startWeaponTaken;
        public string startWeaponId;
        public string[] itemIds = new string[0];
        public string[] weaponIds = new string[0];
        public int[] weaponAmmo = new int[0];
        public int currentWeaponSlot;
        public RunRoomSave[] rooms = new RunRoomSave[0];
    }

    /// <summary>读写 run_save.json 的简单持久化层。</summary>
    public static class RunSaveService
    {
        public const int CurrentVersion = 1;
        public const string FileName = "run_save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave
        {
            get
            {
                try
                {
                    return File.Exists(SavePath);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool TrySave(RunSaveData data)
        {
            if (data == null) return false;

            try
            {
                data.version = CurrentVersion;
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                Debug.Log($"[Save] 单局进度已保存：{SavePath}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] 保存单局进度失败：{exception.Message}");
                return false;
            }
        }

        public static bool TryLoad(out RunSaveData data)
        {
            data = null;

            try
            {
                if (!File.Exists(SavePath)) return false;

                string json = File.ReadAllText(SavePath);
                RunSaveData loaded = JsonUtility.FromJson<RunSaveData>(json);
                if (loaded == null || loaded.version != CurrentVersion)
                {
                    Debug.LogWarning("[Save] 单局存档版本不兼容，已删除旧存档。");
                    Delete();
                    return false;
                }

                data = loaded;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] 读取单局进度失败：{exception.Message}");
                Delete();
                return false;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Save] 删除单局存档失败：{exception.Message}");
            }
        }
    }
}
