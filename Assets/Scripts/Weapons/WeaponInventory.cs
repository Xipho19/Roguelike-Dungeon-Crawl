using System;
using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Weapons
{
    /// <summary>
    /// 玩家的两格武器背包，以及弹匣/装填状态。
    ///
    /// 默认持有基础眼泪武器；F7/F8/F9 可在编辑器或开发构建里快速获得
    /// 法杖、步枪、武士刀，用于验证三种手感。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponInventory : MonoBehaviour
    {
        public const int SlotCount = 2;
        public const string DefaultWeaponPath = "Weapons/Weapon_Tears";

        [SerializeField] private WeaponDefinition startingWeapon;

        [Tooltip("调试获得武器时使用的 Resources 路径；留空时使用默认路径")]
        [SerializeField] private string staffPath = "Weapons/Weapon_Staff";
        [SerializeField] private string riflePath = "Weapons/Weapon_Rifle";
        [SerializeField] private string katanaPath = "Weapons/Weapon_Katana";

        private readonly WeaponDefinition[] _slots = new WeaponDefinition[SlotCount];
        private readonly int[] _ammo = new int[SlotCount];

        private PlayerHealth _health;
        private RunManager _runManager;
        private int _currentSlot;
        private bool _reloading;
        private float _reloadEndTime;
        private bool _resetOnNextRestart;

        public event Action Changed;

        public WeaponDefinition Current
        {
            get
            {
                EnsureSlotsInitialized();
                return _slots[_currentSlot];
            }
        }
        public int CurrentSlot => _currentSlot;
        public bool IsReloading => _reloading;
        public bool UsesMagazine => Current != null && Current.UsesMagazine;

        public int Ammo => AmmoAt(_currentSlot);
        public int MagazineSize => MagazineAt(_currentSlot);

        public float ReloadRemaining =>
            _reloading ? Mathf.Max(0f, _reloadEndTime - Time.time) : 0f;

        public float ReloadProgress
        {
            get
            {
                if (!_reloading || Current == null || Current.reloadTime <= 0f) return 0f;
                return 1f - Mathf.Clamp01(ReloadRemaining / Current.reloadTime);
            }
        }

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            _runManager = FindFirstObjectByType<RunManager>();

            if (_health != null) _health.Died += HandlePlayerDied;
            if (_runManager != null) _runManager.RunRestarted += HandleRunRestarted;

            ResolveStartingWeapon();
            EnsureSlotsInitialized();

            if (GetComponent<WeaponHud>() == null)
                gameObject.AddComponent<WeaponHud>();
        }

        private void OnEnable()
        {
            // 热重载可能丢失非序列化槽位；重新启用时兜底恢复初始泪滴。
            EnsureSlotsInitialized();
        }

        private WeaponDefinition ResolveStartingWeapon()
        {
            if (startingWeapon == null)
                startingWeapon = Resources.Load<WeaponDefinition>(DefaultWeaponPath);

            if (startingWeapon == null)
            {
                Debug.LogWarning(
                    "[Weapon] 找不到基础武器资产，已使用运行时回退泪滴。" +
                    "请执行 Tools/Roguelike/生成武器资产。");
                startingWeapon = CreateFallbackTears();
            }

            return startingWeapon;
        }

        private void EnsureSlotsInitialized()
        {
            if (_slots[0] != null || _slots[1] != null) return;

            WeaponDefinition start = ResolveStartingWeapon();
            _slots[0] = start;
            _ammo[0] = MagazineFor(start);
            _currentSlot = 0;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= HandlePlayerDied;
            if (_runManager != null) _runManager.RunRestarted -= HandleRunRestarted;
        }

        private void Update()
        {
            EnsureSlotsInitialized();
            UpdateReload();

            if (!CanReadInput()) return;

            HandleSelectionInput();
            HandleReloadInput();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDebugWeaponKeys();
#endif
        }

        private bool CanReadInput()
        {
            if (Time.timeScale <= 0.001f) return false;
            if (MapUI.IsOpen) return false;
            if (CombatDebugHud.IsInventoryOpen) return false;
            if (_health != null && _health.IsDead) return false;
            if (_runManager != null && _runManager.State != RunManager.RunState.Playing) return false;
            return true;
        }

        private void HandleSelectionInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame) SelectNext();

            if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Q)) SelectNext();
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
#endif
        }

        private void HandleReloadInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                StartReload();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R)) StartReload();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDebugWeaponKeys()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f7Key.wasPressedThisFrame) GrantFromResources(staffPath);
            else if (keyboard.f8Key.wasPressedThisFrame) GrantFromResources(riflePath);
            else if (keyboard.f9Key.wasPressedThisFrame) GrantFromResources(katanaPath);
            else if (keyboard.f10Key.wasPressedThisFrame)
            {
                ResetToStarting();
                AudioService.Play("bookOpen", 0.6f);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F7)) GrantFromResources(staffPath);
            else if (Input.GetKeyDown(KeyCode.F8)) GrantFromResources(riflePath);
            else if (Input.GetKeyDown(KeyCode.F9)) GrantFromResources(katanaPath);
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                ResetToStarting();
                AudioService.Play("bookOpen", 0.6f);
            }
#endif
        }
#endif

        private void GrantFromResources(string path)
        {
            WeaponDefinition weapon = Resources.Load<WeaponDefinition>(path);
            if (weapon == null)
            {
                Debug.LogWarning($"[Weapon] 找不到武器资产 Resources/{path}");
                return;
            }

            Equip(weapon);
            AudioService.Play("bookOpen", 0.65f);
        }

        public WeaponDefinition SlotAt(int index)
        {
            EnsureSlotsInitialized();
            return index >= 0 && index < SlotCount ? _slots[index] : null;
        }

        public int AmmoAt(int index)
        {
            return index >= 0 && index < SlotCount ? _ammo[index] : 0;
        }

        public int MagazineAt(int index)
        {
            return MagazineFor(SlotAt(index));
        }

        public bool IsSlotReloading(int index)
        {
            return _reloading && index == _currentSlot;
        }

        public bool Owns(WeaponDefinition weapon)
        {
            if (weapon == null) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == weapon) return true;
            }

            return false;
        }

        /// <summary>装备一件武器：已有则切换，否则优先放入空槽，再替换当前槽。</summary>
        public bool Equip(WeaponDefinition weapon)
        {
            if (weapon == null) return false;

            int existing = IndexOf(weapon);
            if (existing >= 0)
            {
                SelectSlot(existing);
                return false;
            }

            int empty = IndexOf(null);
            int slot = empty >= 0 ? empty : _currentSlot;

            _slots[slot] = weapon;
            _ammo[slot] = MagazineFor(weapon);
            _currentSlot = slot;
            CancelReload();
            Changed?.Invoke();
            return true;
        }

        public void SelectNext()
        {
            if (_slots[0] == null && _slots[1] == null) return;

            int next = _currentSlot;
            for (int step = 1; step <= SlotCount; step++)
            {
                int candidate = (_currentSlot + step) % SlotCount;
                if (_slots[candidate] == null) continue;
                next = candidate;
                break;
            }

            SelectSlot(next);
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            if (index == _currentSlot) return;
            if (_slots[index] == null) return;

            _currentSlot = index;
            CancelReload();
            Changed?.Invoke();
        }

        /// <summary>开始装填；非弹匣武器、满弹或正在装填时返回 false。</summary>
        public bool StartReload()
        {
            WeaponDefinition weapon = Current;
            if (weapon == null || !weapon.UsesMagazine) return false;
            if (_reloading || _ammo[_currentSlot] >= weapon.magazineSize) return false;
            if (weapon.reloadTime <= 0.01f)
            {
                _ammo[_currentSlot] = weapon.magazineSize;
                Changed?.Invoke();
                return true;
            }

            _reloading = true;
            _reloadEndTime = Time.time + weapon.reloadTime;
            AudioService.Play("metalClick", 0.45f);
            Changed?.Invoke();
            return true;
        }

        public bool TryConsumeRound()
        {
            WeaponDefinition weapon = Current;
            if (weapon == null) return false;
            if (!weapon.UsesMagazine) return true;
            if (_reloading || _ammo[_currentSlot] <= 0) return false;

            _ammo[_currentSlot]--;
            return true;
        }

        private void UpdateReload()
        {
            if (!_reloading) return;
            if (Time.time < _reloadEndTime) return;

            _reloading = false;
            WeaponDefinition weapon = Current;
            if (weapon != null && weapon.UsesMagazine)
                _ammo[_currentSlot] = weapon.magazineSize;

            AudioService.Play("metalLatch", 0.5f);
            Changed?.Invoke();
        }

        private void CancelReload()
        {
            _reloading = false;
            _reloadEndTime = 0f;
        }

        private int IndexOf(WeaponDefinition weapon)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == weapon) return i;
            }

            return -1;
        }

        private void HandlePlayerDied()
        {
            // 等 RunManager 把整局重置完成后再清空武器，避免死亡画面还在读旧槽位。
            _resetOnNextRestart = true;
        }

        private void HandleRunRestarted()
        {
            if (_resetOnNextRestart)
            {
                _resetOnNextRestart = false;
                ResetToStarting();
                return;
            }

            // 胜利后保留武器进入下一局，但把弹匣补满。
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] != null)
                    _ammo[i] = MagazineFor(_slots[i]);
            }

            CancelReload();
            Changed?.Invoke();
        }

        public void ResetToStarting()
        {
            _slots[0] = ResolveStartingWeapon();
            _slots[1] = null;
            _ammo[0] = MagazineFor(_slots[0]);
            _ammo[1] = 0;
            _currentSlot = 0;
            CancelReload();
            Changed?.Invoke();
        }

        /// <summary>读档时恢复两个武器槽、剩余弹药与当前槽位。</summary>
        public void RestoreFromSave(
            WeaponDefinition slot0,
            int ammo0,
            WeaponDefinition slot1,
            int ammo1,
            int currentSlot)
        {
            _slots[0] = slot0;
            _slots[1] = slot1;
            _ammo[0] = slot0 != null ? Mathf.Clamp(ammo0, 0, MagazineFor(slot0)) : 0;
            _ammo[1] = slot1 != null ? Mathf.Clamp(ammo1, 0, MagazineFor(slot1)) : 0;

            if (currentSlot == 1 && slot1 != null)
                _currentSlot = 1;
            else if (slot0 != null)
                _currentSlot = 0;
            else if (slot1 != null)
                _currentSlot = 1;
            else
                _currentSlot = 0;

            CancelReload();
            EnsureSlotsInitialized();
            Changed?.Invoke();
        }

        private static int MagazineFor(WeaponDefinition weapon)
        {
            return weapon != null && weapon.UsesMagazine ? weapon.magazineSize : 0;
        }

        private static WeaponDefinition CreateFallbackTears()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.name = "Weapon_Tears_Fallback";
            weapon.weaponName = "泪滴";
            weapon.description = "最基础的自动弹丸。";
            weapon.kind = WeaponKind.Tears;
            weapon.damage = 1f;
            weapon.fireInterval = 0.24f;
            weapon.projectileSpeed = 14f;
            weapon.projectileLifetime = 1f;
            weapon.projectileScale = 0.36f;
            weapon.fireSound = "tear_shot";
            weapon.fireVolume = 0.55f;
            return weapon;
        }
    }
}
