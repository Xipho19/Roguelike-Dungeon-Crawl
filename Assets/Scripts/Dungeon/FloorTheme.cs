using Roguelike.Combat;
using UnityEngine;

namespace Roguelike.Dungeon
{
    public enum FloorBossVisual
    {
        Ogre = 0,
        BigZombie = 1,
        BigDemon = 2,
    }

    /// <summary>
    /// 一层地牢的视觉主题：名称、地板/墙体染色、相机背景。
    /// 资产放在 Resources/FloorThemes，由 RunManager 按 order 排序后循环使用。
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Floor Theme", fileName = "FloorTheme_New")]
    public sealed class FloorTheme : ScriptableObject
    {
        [Header("展示")]
        public string displayName = "地牢";

        [Tooltip("越小越靠前；相同 order 按资产名排序")]
        public int order;

        [Header("配色")]
        [Tooltip("与地板贴图相乘的染色")]
        public Color floorTint = Color.white;

        [Tooltip("与墙体贴图相乘的染色")]
        public Color wallTint = Color.white;

        [Tooltip("相机纯色背景")]
        public Color cameraBackground = new Color(0.055f, 0.05f, 0.075f, 1f);

        [Header("Boss")]
        [Tooltip("该主题楼层使用的 Boss 形象")]
        public FloorBossVisual bossVisual = FloorBossVisual.BigDemon;

        [Header("小怪池")]
        [Tooltip("该楼层普通战斗房和挑战房使用的小怪；留空时回退到 Flow 的默认六种")]
        public Enemy[] commonEnemies = new Enemy[0];

        [Header("BGM")]
        [Tooltip("该楼层循环播放的背景音乐；留空则不播放")]
        public AudioClip bgm;

        [Range(0f, 1f)]
        [Tooltip("背景音乐音量")]
        public float bgmVolume = 0.5f;
    }
}
