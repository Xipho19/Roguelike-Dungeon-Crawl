using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.UI;
using UnityEngine;

namespace Roguelike.Weapons
{
    /// <summary>右下角的两格武器栏：当前武器、弹药/装填状态和切换提示。</summary>
    public sealed class WeaponHud : MonoBehaviour
    {
        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private RunManager runManager;

        private const float PanelWidth = 214f;
        private const float RowHeight = 52f;
        private const float RowGap = 6f;
        private const float HeaderHeight = 24f;
        private const float Margin = 14f;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<WeaponInventory>();
            if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        }

        private void OnGUI()
        {
            if (inventory == null) return;
            if (Time.timeScale <= 0.001f) return;
            if (MapUI.IsOpen) return;
            if (runManager != null && runManager.State != RunManager.RunState.Playing) return;

            float height = HeaderHeight + RowHeight * 2f + RowGap + 14f;
            bool bossBarVisible = BossEnemy.Active != null
                && !BossEnemy.Active.IsDead
                && BossEnemy.Active.CombatActive;

            float bottom = Screen.height - height - (bossBarVisible ? 84f : Margin);
            var panel = new Rect(
                Screen.width - PanelWidth - Margin,
                bottom,
                PanelWidth,
                height);

            UiTheme.DrawPanel(panel);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 10f, panel.y + 3f, 88f, HeaderHeight),
                "武器",
                UiTheme.Small,
                UiTheme.GoldBright);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 92f, panel.y + 4f, panel.width - 100f, HeaderHeight),
                "Q 切换 · R 装填",
                UiTheme.Tiny,
                UiTheme.Muted);

            for (int i = 0; i < WeaponInventory.SlotCount; i++)
            {
                Rect row = new Rect(
                    panel.x + 8f,
                    panel.y + HeaderHeight + i * (RowHeight + RowGap),
                    panel.width - 16f,
                    RowHeight);

                DrawSlot(row, i);
            }
        }

        private void DrawSlot(Rect row, int index)
        {
            bool selected = index == inventory.CurrentSlot;
            WeaponDefinition weapon = inventory.SlotAt(index);

            UiTheme.DrawInset(row);
            if (selected)
                UiTheme.Fill(new Rect(row.x + 2f, row.y + 2f, 3f, row.height - 4f), UiTheme.GoldBright);
            else
                UiTheme.Fill(new Rect(row.x + 2f, row.y + 2f, 3f, row.height - 4f), UiTheme.PanelEdge);

            var key = new Rect(row.x + 9f, row.y + 8f, 16f, 16f);
            UiTheme.DrawKeyBadge(key, (index + 1).ToString());

            var iconRect = new Rect(row.x + 31f, row.y + 8f, 36f, 36f);
            UiTheme.DrawIconSlot(iconRect, weapon != null ? weapon.icon : null, selected ? UiTheme.GoldBright : UiTheme.GoldDim);

            if (weapon == null)
            {
                UiTheme.DrawLabelShadow(
                    new Rect(row.x + 75f, row.y + 7f, row.width - 80f, 20f),
                    "空武器槽",
                    UiTheme.Small,
                    UiTheme.Muted);
                UiTheme.DrawLabelShadow(
                    new Rect(row.x + 75f, row.y + 27f, row.width - 80f, 18f),
                    "拾取武器后自动装备",
                    UiTheme.Tiny,
                    UiTheme.GoldDim);
                return;
            }

            Color nameColor = selected ? UiTheme.Text : UiTheme.Muted;
            UiTheme.DrawLabelShadow(
                new Rect(row.x + 75f, row.y + 5f, row.width - 80f, 20f),
                weapon.weaponName,
                UiTheme.Small,
                nameColor);

            string detail = GetDetailText(weapon, index, selected);
            Color detailColor = selected ? UiTheme.GoldBright : UiTheme.Muted;
            UiTheme.DrawLabelShadow(
                new Rect(row.x + 75f, row.y + 27f, row.width - 80f, 18f),
                detail,
                UiTheme.Tiny,
                detailColor);

            if (selected && inventory.IsSlotReloading(index))
            {
                UiTheme.DrawBar(
                    new Rect(row.x + 75f, row.y + 44f, row.width - 84f, 4f),
                    inventory.ReloadProgress,
                    UiTheme.Gold,
                    false);
            }
        }

        private string GetDetailText(WeaponDefinition weapon, int index, bool selected)
        {
            if (weapon.IsMelee)
            {
                return weapon.kind == WeaponKind.Waraxe
                    ? "近战 · 二段旋斩"
                    : "近战 · 三段斩";
            }

            if (!weapon.UsesMagazine)
            {
                return weapon.kind == WeaponKind.Staff || weapon.kind == WeaponKind.EmberStaff
                    ? "法杖 · 无限"
                    : "自动 · 无限";
            }

            if (selected && inventory.IsSlotReloading(index))
                return $"装填中 {inventory.ReloadRemaining:0.0}s";

            int ammo = inventory.AmmoAt(index);
            int magazine = inventory.MagazineAt(index);
            return ammo <= 0 ? $"0/{magazine} · R 装填" : $"{ammo}/{magazine}";
        }
    }
}
