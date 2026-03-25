using UnityEngine;

namespace DRAUM.Modules.Combat.Animation
{
    public class FPHandsAnimatorEventProxy : MonoBehaviour
    {
        [Tooltip("Ссылка на ForceGripAbility (на игроке).")]
        public DRAUM.Modules.Combat.Abilities.ForceGripAbility forceGripAbility;

        /// <summary>
        /// Прокси-метод для Animation Event. Вызывается из анимации FPHands Attack-Amulet.
        /// </summary>
        public void OnAttackAnimationCompleteProxy()
        {
            if (forceGripAbility != null)
            {
                forceGripAbility.OnAttackAnimationComplete();
            }
            else
            {
                Debug.LogWarning("[FPHandsAnimatorEventProxy] ForceGripAbility не назначен в прокси!");
            }
        }
    }
}
