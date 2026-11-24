using System.Collections.Generic;
using Roguelike.Weapons;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 生成武器系统的初始资产。
    /// 菜单：Tools / Roguelike / 生成武器资产
    /// </summary>
    public static class WeaponAssetFactory
    {
        private const string Folder = "Assets/Resources/Weapons";
        private const string Art = "Assets/Art/Weapons/MVP/";
        private const string Tiles = "Assets/Art/ThirdParty/0x72_DungeonTilesetII/";

        [MenuItem("Tools/Roguelike/生成武器资产")]
        public static void GenerateFromMenu()
        {
            EnsureWeapons();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Weapon] 已生成 8 件武器资产。");
        }

        public static void EnsureWeapons()
        {
            EnsureFolder(Folder);

            WeaponDefinition tears = LoadOrCreate("Weapon_Tears");
            tears.weaponName = "泪滴";
            tears.description = "最基础的自动泪滴。射速快，适配所有眼泪类自动道具。";
            tears.kind = WeaponKind.Tears;
            tears.icon = LoadSprite(Tiles + "frames/tear_blue.png");
            tears.rarity = WeaponRarity.Common;
            tears.dropWeight = 0;
            tears.shopPrice = 0;
            tears.damage = 1f;
            tears.fireInterval = 0.24f;
            tears.projectileSpeed = 14f;
            tears.projectileLifetime = 1f;
            tears.pierce = 0;
            tears.projectiles = 1;
            tears.spreadStep = 8f;
            tears.projectileFrames = Frames(Tiles + "frames/tear_blue.png");
            tears.projectileScale = 0.36f;
            tears.projectileColor = Color.white;
            tears.rotateProjectile = false;
            tears.magazineSize = 0;
            tears.reloadTime = 0f;
            tears.heldSprite = null;
            tears.heldDistance = 0f;
            tears.heldSideOffset = 0f;
            tears.heldVerticalOffset = 0f;
            ConfigureSounds(tears, "tear_shot", 0.55f);
            Save(tears);

            WeaponDefinition staff = LoadOrCreate("Weapon_Staff");
            staff.weaponName = "绿宝石法杖";
            staff.description = "发射可穿透的绿色能量球。单发伤害高，射速较慢。";
            staff.kind = WeaponKind.Staff;
            staff.icon = LoadSprite(Art + "icon_staff.png");
            staff.rarity = WeaponRarity.Rare;
            staff.dropWeight = 20;
            staff.shopPrice = 14;
            staff.damage = 2.2f;
            staff.fireInterval = 0.52f;
            staff.projectileSpeed = 9f;
            staff.projectileLifetime = 1.6f;
            staff.pierce = 1;
            staff.projectiles = 1;
            staff.spreadStep = 8f;
            staff.projectileFrames = Frames(
                Art + "orb_energy_0.png",
                Art + "orb_energy_1.png",
                Art + "orb_energy_2.png",
                Art + "orb_energy_3.png");
            staff.projectileScale = 0.68f;
            staff.projectileColor = Color.white;
            staff.rotateProjectile = false;
            staff.magazineSize = 0;
            staff.reloadTime = 0f;
            staff.heldSprite = LoadSprite(Tiles + "weapons/weapon_green_magic_staff.png");
            staff.heldScale = 0.65f;
            staff.heldDistance = 0.15f;
            staff.heldSideOffset = 0.12f;
            staff.heldVerticalOffset = -0.30f;
            staff.heldAngleOffset = -90f;
            staff.flipHeldWhenAimingLeft = false;
            ConfigureSounds(staff, "weapon_staff_zap", 0.50f);
            Save(staff);

            WeaponDefinition rifle = LoadOrCreate("Weapon_Rifle");
            rifle.weaponName = "突击步枪";
            rifle.description = "高速连射，18 发弹匣。按 R 装填，适合持续输出。";
            rifle.kind = WeaponKind.Rifle;
            rifle.icon = LoadSprite(Art + "icon_rifle.png");
            rifle.rarity = WeaponRarity.Rare;
            rifle.dropWeight = 20;
            rifle.shopPrice = 16;
            rifle.damage = 0.8f;
            rifle.fireInterval = 0.11f;
            rifle.projectileSpeed = 21f;
            rifle.projectileLifetime = 0.8f;
            rifle.pierce = 0;
            rifle.projectiles = 1;
            rifle.spreadStep = 2.5f;
            rifle.projectileFrames = Frames(Art + "bullet_rifle.png");
            rifle.projectileScale = 0.8f;
            rifle.projectileColor = Color.white;
            rifle.rotateProjectile = true;
            rifle.magazineSize = 18;
            rifle.reloadTime = 1.2f;
            rifle.heldSprite = LoadSprite(Art + "weapon_rifle_held.png");
            rifle.heldScale = 0.9f;
            rifle.heldDistance = 0.18f;
            rifle.heldSideOffset = 0.10f;
            rifle.heldVerticalOffset = -0.30f;
            rifle.heldAngleOffset = 0f;
            rifle.flipHeldWhenAimingLeft = true;
            ConfigureSounds(rifle, "weapon_rifle_shot", 0.38f);
            Save(rifle);

            WeaponDefinition katana = LoadOrCreate("Weapon_Katana");
            katana.weaponName = "武士刀";
            katana.description = "前方扇形三段斩，第三段强化。享受通用加伤、暴击与中毒。";
            katana.kind = WeaponKind.Katana;
            katana.icon = LoadSprite(Art + "icon_katana.png");
            katana.rarity = WeaponRarity.Legendary;
            katana.dropWeight = 10;
            katana.shopPrice = 24;
            katana.damage = 3.2f;
            katana.fireInterval = 0.40f;
            katana.projectileSpeed = 14f;
            katana.projectileLifetime = 1f;
            katana.pierce = 0;
            katana.projectiles = 1;
            katana.spreadStep = 0f;
            katana.projectileFrames = null;
            katana.projectileScale = 0.36f;
            katana.projectileColor = Color.white;
            katana.rotateProjectile = false;
            katana.magazineSize = 0;
            katana.reloadTime = 0f;
            katana.meleeRadius = 2.15f;
            katana.meleeArc = 115f;
            katana.meleeKnockback = 7f;
            katana.comboHits = 3;
            katana.comboWindow = 1.25f;
            katana.comboFinisherScale = 1.55f;
            katana.slashFrames = Frames(
                Art + "katana_slash_0.png",
                Art + "katana_slash_1.png",
                Art + "katana_slash_2.png");
            katana.slashScale = 1.35f;
            katana.heldSprite = LoadSprite(Tiles + "weapons/weapon_katana.png");
            katana.heldScale = 0.65f;
            katana.heldDistance = 0.16f;
            katana.heldSideOffset = 0.12f;
            katana.heldVerticalOffset = -0.30f;
            katana.heldAngleOffset = -90f;
            katana.flipHeldWhenAimingLeft = false;
            ConfigureSounds(
                katana,
                "weapon_katana_slash", 0.78f,
                "weapon_katana_slash2", 0.74f,
                "weapon_katana_finisher", 0.95f);
            Save(katana);

            WeaponDefinition shotgun = LoadOrCreate("Weapon_Shotgun");
            shotgun.weaponName = "霰弹枪";
            shotgun.description = "一次射出 6 发散射弹丸，近距离爆发极高。";
            shotgun.kind = WeaponKind.Shotgun;
            shotgun.icon = LoadSprite(Art + "icon_shotgun.png");
            shotgun.rarity = WeaponRarity.Rare;
            shotgun.dropWeight = 20;
            shotgun.shopPrice = 18;
            shotgun.damage = 0.55f;
            shotgun.fireInterval = 0.78f;
            shotgun.projectileSpeed = 18f;
            shotgun.projectileLifetime = 0.55f;
            shotgun.pierce = 0;
            shotgun.projectiles = 6;
            shotgun.spreadStep = 11f;
            shotgun.projectileFrames = Frames(Art + "pellet_shotgun.png");
            shotgun.projectileScale = 0.72f;
            shotgun.projectileColor = Color.white;
            shotgun.rotateProjectile = true;
            shotgun.projectileExplosionRadius = 0f;
            shotgun.projectileExplosionDamage = 0f;
            shotgun.magazineSize = 6;
            shotgun.reloadTime = 1.6f;
            shotgun.heldSprite = LoadSprite(Art + "weapon_shotgun_held.png");
            shotgun.heldScale = 0.9f;
            shotgun.heldDistance = 0.18f;
            shotgun.heldSideOffset = 0.10f;
            shotgun.heldVerticalOffset = -0.30f;
            shotgun.heldAngleOffset = 0f;
            shotgun.flipHeldWhenAimingLeft = true;
            ConfigureSounds(shotgun, "weapon_shotgun_blast", 0.58f);
            Save(shotgun);

            WeaponDefinition crossbow = LoadOrCreate("Weapon_Crossbow");
            crossbow.weaponName = "十字弩";
            crossbow.description = "发射高伤害穿透弩箭，弹匣 5 发，适合中距离点杀。";
            crossbow.kind = WeaponKind.Crossbow;
            crossbow.icon = LoadSprite(Art + "icon_crossbow.png");
            crossbow.rarity = WeaponRarity.Rare;
            crossbow.dropWeight = 20;
            crossbow.shopPrice = 18;
            crossbow.damage = 4.5f;
            crossbow.fireInterval = 0.75f;
            crossbow.projectileSpeed = 17f;
            crossbow.projectileLifetime = 1.2f;
            crossbow.pierce = 2;
            crossbow.projectiles = 1;
            crossbow.spreadStep = 0f;
            crossbow.projectileFrames = Frames(Art + "bolt_crossbow.png");
            crossbow.projectileScale = 0.9f;
            crossbow.projectileColor = Color.white;
            crossbow.rotateProjectile = true;
            crossbow.projectileExplosionRadius = 0f;
            crossbow.projectileExplosionDamage = 0f;
            crossbow.magazineSize = 5;
            crossbow.reloadTime = 1.4f;
            crossbow.heldSprite = LoadSprite(Art + "weapon_crossbow_held.png");
            crossbow.heldScale = 0.9f;
            crossbow.heldDistance = 0.18f;
            crossbow.heldSideOffset = 0.10f;
            crossbow.heldVerticalOffset = -0.30f;
            crossbow.heldAngleOffset = 0f;
            crossbow.flipHeldWhenAimingLeft = true;
            ConfigureSounds(crossbow, "weapon_crossbow_shot", 0.72f);
            Save(crossbow);

            WeaponDefinition ember = LoadOrCreate("Weapon_EmberStaff");
            ember.weaponName = "烈焰法杖";
            ember.description = "发射缓慢的火焰球，命中时产生小范围爆炸。";
            ember.kind = WeaponKind.EmberStaff;
            ember.icon = LoadSprite(Art + "icon_ember_staff.png");
            ember.rarity = WeaponRarity.Rare;
            ember.dropWeight = 15;
            ember.shopPrice = 20;
            ember.damage = 2.8f;
            ember.fireInterval = 0.70f;
            ember.projectileSpeed = 8.5f;
            ember.projectileLifetime = 1.5f;
            ember.pierce = 0;
            ember.projectiles = 1;
            ember.spreadStep = 0f;
            ember.projectileFrames = Frames(
                Art + "ember_orb_0.png",
                Art + "ember_orb_1.png",
                Art + "ember_orb_2.png",
                Art + "ember_orb_3.png");
            ember.projectileScale = 0.78f;
            ember.projectileColor = Color.white;
            ember.rotateProjectile = false;
            ember.projectileExplosionRadius = 1.5f;
            ember.projectileExplosionDamage = 1.2f;
            ember.magazineSize = 0;
            ember.reloadTime = 0f;
            ember.heldSprite = LoadSprite(Tiles + "weapons/weapon_red_magic_staff.png");
            ember.heldScale = 0.65f;
            ember.heldDistance = 0.15f;
            ember.heldSideOffset = 0.12f;
            ember.heldVerticalOffset = -0.30f;
            ember.heldAngleOffset = -90f;
            ember.flipHeldWhenAimingLeft = false;
            ConfigureSounds(ember, "weapon_ember_cast", 0.92f);
            Save(ember);

            WeaponDefinition waraxe = LoadOrCreate("Weapon_Waraxe");
            waraxe.weaponName = "战斧";
            waraxe.description = "双手战斧的全周旋斩，第二段强化重击，拥有极高击退。";
            waraxe.kind = WeaponKind.Waraxe;
            waraxe.icon = LoadSprite(Art + "icon_waraxe.png");
            waraxe.rarity = WeaponRarity.Legendary;
            waraxe.dropWeight = 10;
            waraxe.shopPrice = 26;
            waraxe.damage = 4.8f;
            waraxe.fireInterval = 0.65f;
            waraxe.projectileSpeed = 14f;
            waraxe.projectileLifetime = 1f;
            waraxe.pierce = 0;
            waraxe.projectiles = 1;
            waraxe.spreadStep = 0f;
            waraxe.projectileFrames = null;
            waraxe.projectileScale = 0.36f;
            waraxe.projectileColor = Color.white;
            waraxe.rotateProjectile = false;
            waraxe.projectileExplosionRadius = 0f;
            waraxe.projectileExplosionDamage = 0f;
            waraxe.magazineSize = 0;
            waraxe.reloadTime = 0f;
            waraxe.meleeRadius = 2.35f;
            waraxe.meleeArc = 360f;
            waraxe.meleeKnockback = 10f;
            waraxe.comboHits = 2;
            waraxe.comboWindow = 1.0f;
            waraxe.comboFinisherScale = 1.35f;
            waraxe.slashFrames = Frames(
                Art + "katana_slash_0.png",
                Art + "katana_slash_1.png",
                Art + "katana_slash_2.png");
            waraxe.slashScale = 1.55f;
            waraxe.heldSprite = LoadSprite(Art + "weapon_waraxe_held.png");
            waraxe.heldScale = 0.62f;
            waraxe.heldDistance = 0.14f;
            waraxe.heldSideOffset = 0.08f;
            waraxe.heldVerticalOffset = -0.32f;
            waraxe.heldAngleOffset = -90f;
            waraxe.flipHeldWhenAimingLeft = false;
            ConfigureSounds(
                waraxe,
                "weapon_waraxe_swing", 0.64f,
                null, 0f,
                "weapon_waraxe_finisher", 0.72f);
            Save(waraxe);
        }

        private static void ConfigureSounds(
            WeaponDefinition weapon,
            string fireSound,
            float fireVolume,
            string altSound = null,
            float altVolume = 0f,
            string finisherSound = null,
            float finisherVolume = 0f)
        {
            weapon.fireSound = fireSound;
            weapon.fireVolume = fireVolume;
            weapon.altSound = altSound;
            weapon.altVolume = altVolume > 0f ? altVolume : fireVolume;
            weapon.finisherSound = finisherSound;
            weapon.finisherVolume = finisherVolume > 0f ? finisherVolume : fireVolume;
        }

        private static WeaponDefinition LoadOrCreate(string assetName)
        {
            string path = $"{Folder}/{assetName}.asset";
            WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(weapon, path);
            }

            return weapon;
        }

        private static void Save(WeaponDefinition weapon)
        {
            EditorUtility.SetDirty(weapon);
            AssetDatabase.SaveAssets();
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[Weapon] 找不到 Sprite：{path}");
            return sprite;
        }

        private static Sprite[] Frames(params string[] paths)
        {
            var frames = new List<Sprite>(paths.Length);
            for (int i = 0; i < paths.Length; i++)
            {
                Sprite sprite = LoadSprite(paths[i]);
                if (sprite != null) frames.Add(sprite);
            }

            return frames.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
