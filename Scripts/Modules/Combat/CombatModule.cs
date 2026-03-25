using DRAUM.Core;
using DRAUM.Modules.Combat.Animation;
using DRAUM.Modules.Combat.Abilities;
using DRAUM.Modules.Combat.Events;
using DRAUM.Modules.Player;
using DRAUM.Modules.Player.Events;
using DRAUM.Core.Infrastructure.Logger;
using UnityEngine;

namespace DRAUM.Modules.Combat
{
    /// <summary>
    /// CombatModule - связующий модуль боёвки для Core-архитектуры.
    ///
    /// ВАЖНО:
    /// - Этот модуль не переписывает логику ударов/анимаций (её делают FPMAxe, WeaponCollisionDetector, BrutalityController и т.д.).
    /// - Он отвечает за "оркестровку" боевых компонентов: включать/выключать их в нужные моменты (например, когда игрок заблокирован инвентарём/добиванием),
    ///   чтобы боёвка не превращалась в кашу из разрозненных скриптов, которые "всегда слушают input".
    /// </summary>
    public class CombatModule : BehaviorModuleBase
    {
        public override string ModuleName => "Combat";
        public override int InitializationPriority => 35;

        [Header("Damage (from hits)")]
        [Tooltip("Если true, CombatModule будет применять урон при PlayerCombatHitEvent impact-импактах.")]
        public bool applyDamageFromPlayerHits = true;

        [Header("References (optional, can auto-find)")]
        [Tooltip("PlayerModule для чтения состояний блокировки/боевого режима")]
        public PlayerModule playerModule;

        [Tooltip("FPMAxe - основной контроллер ударов/блока игрока")]
        public FPMAxe fpAxe;

        [Tooltip("ForceGripAbility - способность, которая слушает LMB при активном режиме")]
        public ForceGripAbility forceGripAbility;

        // Состояние "были ли компоненты включены до блокировки"
        private bool _fpAxeEnabledBeforeLock;
        private bool _forceGripEnabledBeforeLock;

        private bool _componentsDisabledByLock;

        /// <summary>
        /// Единственная точка применения урона к EnemyEntity.
        /// Всё остальное в боёвке должно маршрутизировать сюда (через события).
        /// </summary>
        public float ApplyEnemyDamage(EnemyEntity targetEnemy, float baseDamage, BodyPart bodyPart, Vector3 hitPoint)
        {
            if (targetEnemy == null) return 0f;
            if (baseDamage <= 0f) return 0f;
            return targetEnemy.TakeDamage(baseDamage, bodyPart, hitPoint);
        }

        protected override void OnInitialize()
        {
            // Автопоиск ссылок, чтобы не ломать сцену.
            if (playerModule == null)
                playerModule = FindFirstObjectByType<PlayerModule>();

            if (fpAxe == null)
                fpAxe = FindFirstObjectByType<FPMAxe>();

            if (forceGripAbility == null)
                forceGripAbility = FindFirstObjectByType<ForceGripAbility>();

            _fpAxeEnabledBeforeLock = fpAxe != null && fpAxe.enabled;
            _forceGripEnabledBeforeLock = forceGripAbility != null && forceGripAbility.enabled;
            _componentsDisabledByLock = false;

            SubscribeToEvents();

            DraumLogger.Info(this, "[CombatModule] Initialized.");
        }

        private void SubscribeToEvents()
        {
            Events.Subscribe<PlayerLockStateChangedEvent>(OnPlayerLockStateChanged);
            Events.Subscribe<PlayerCombatHitEvent>(OnPlayerCombatHit);
            Events.Subscribe<EnemyDamageRequestEvent>(OnEnemyDamageRequest);
        }

        private void UnsubscribeFromEvents()
        {
            if (EventBus.Instance == null) return;
            Events.Unsubscribe<PlayerLockStateChangedEvent>(OnPlayerLockStateChanged);
            Events.Unsubscribe<PlayerCombatHitEvent>(OnPlayerCombatHit);
            Events.Unsubscribe<EnemyDamageRequestEvent>(OnEnemyDamageRequest);
        }

        private void OnPlayerLockStateChanged(PlayerLockStateChangedEvent evt)
        {
            // Логика: если игрок заблокирован (инвентарь/добивание/и т.п.), выключаем боевые компоненты.
            // У FPMAxe/ForceGripAbility есть Input-зависимость (LMB/RMB и т.д.), поэтому gating нужен именно тут.
            if (evt == null) return;

            bool locked = evt.IsLocked;

            if (locked && !_componentsDisabledByLock)
            {
                if (fpAxe != null)
                {
                    _fpAxeEnabledBeforeLock = fpAxe.enabled;
                    fpAxe.enabled = false;
                    // На всякий случай сбрасываем анимационные bool'ы/состояние входа.
                    fpAxe.ResetMouseX();
                }

                if (forceGripAbility != null)
                {
                    _forceGripEnabledBeforeLock = forceGripAbility.enabled;
                    forceGripAbility.enabled = false;
                }

                _componentsDisabledByLock = true;
            }
            else if (!locked && _componentsDisabledByLock)
            {
                if (fpAxe != null)
                    fpAxe.enabled = _fpAxeEnabledBeforeLock;

                if (forceGripAbility != null)
                    forceGripAbility.enabled = _forceGripEnabledBeforeLock;

                _componentsDisabledByLock = false;
            }
        }

        private void OnPlayerCombatHit(PlayerCombatHitEvent evt)
        {
            if (evt == null) return;
            if (!applyDamageFromPlayerHits) return;
            if (!evt.IsImpact) return;
            if (_componentsDisabledByLock) return;

            if (evt.HitCollider == null) return;

            // EnemyEntity живёт в DeprecatedScripts, но реально используется в боёвке.
            EnemyEntity enemy = evt.HitCollider.GetComponentInParent<EnemyEntity>();
            if (enemy == null || !enemy.IsAlive) return;

            BodyPart bodyPart = BodyPartMapper.GetBodyPart(evt.HitCollider);

            // Базовый урон синхронизирован с тем же "силовым" смыслом, что используется в импакт/force-пайплайне.
            // EnemyEntity сам применит body-part множители к baseDamage.
            float hitSpeed = Mathf.Max(0f, evt.HitSpeed);
            float weaponMass = Mathf.Max(0f, evt.WeaponMass);
            float baseForceMultiplier = Mathf.Max(0f, evt.BaseForceMultiplier);
            float speedForceMultiplier = Mathf.Max(0f, evt.SpeedForceMultiplier);

            float baseDamage = (hitSpeed * speedForceMultiplier + weaponMass) * baseForceMultiplier;
            if (baseDamage <= 0f) return;

            // Маршрутизируем применение урона в единый pipeline через event.
            Events.Publish(new EnemyDamageRequestEvent
            {
                TargetEnemy = enemy,
                BaseDamage = baseDamage,
                BodyPart = bodyPart,
                HitPoint = evt.HitPosition,
                Reply = null
            });
        }

        private void OnEnemyDamageRequest(EnemyDamageRequestEvent evt)
        {
            if (evt == null) return;
            if (evt.TargetEnemy == null) { evt.Reply?.Invoke(0f); return; }

            // Для любых источников урона применение должно идти через единую точку.
            float actualDamage = ApplyEnemyDamage(evt.TargetEnemy, evt.BaseDamage, evt.BodyPart, evt.HitPoint);
            evt.Reply?.Invoke(actualDamage);
        }

        protected override void OnShutdown()
        {
            UnsubscribeFromEvents();

            // На выходе восстанавливаем включение (чтобы не зависнуть в disabled состоянии после выгрузки).
            if (fpAxe != null)
                fpAxe.enabled = _fpAxeEnabledBeforeLock;

            if (forceGripAbility != null)
                forceGripAbility.enabled = _forceGripEnabledBeforeLock;

            playerModule = null;
            fpAxe = null;
            forceGripAbility = null;

            DraumLogger.Info(this, "[CombatModule] Unloaded.");
        }
    }
}

