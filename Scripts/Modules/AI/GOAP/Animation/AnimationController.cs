using UnityEngine;

public abstract class AnimationController : MonoBehaviour {
    const float k_crossfadeDuration = 0.1f;
    
    protected Animator animator;
    CountdownTimer timer;
    
    float animationLength;
    
    [HideInInspector] public int locomotionClip = Animator.StringToHash("Locomotion");
    [HideInInspector] public int speedHash = Animator.StringToHash("Speed");
    [HideInInspector] public int attackClip = Animator.StringToHash("Attack");
    
    void Awake() {
        animator = GetComponentInChildren<Animator>();
        SetLocomotionClip();
        SetAttackClip();
        SetSpeedHash();
    }

    public void SetSpeed(float speed) => animator.SetFloat(speedHash, speed);
    public virtual void Attack() => PlayAnimationUsingTimer(attackClip);
    
    void Update() => timer?.Tick(Time.deltaTime);

    protected void PlayAnimationUsingTimer(int clipHash) {
        float length = GetAnimationLength(clipHash);
        if (length <= 0) {
            Debug.LogWarning($"[AnimationController] Анимация с хешем {clipHash} не найдена или длина равна 0!");
            return;
        }
        
        timer = new CountdownTimer(length);
        timer.OnTimerStart += () => {
            if (animator != null && animator.isActiveAndEnabled) {
                animator.CrossFade(clipHash, k_crossfadeDuration);
            }
        };
        timer.OnTimerStop += () => {
            if (animator != null && animator.isActiveAndEnabled) {
                float locomotionLength = GetAnimationLength(locomotionClip);
                if (locomotionLength > 0) {
                    animator.CrossFade(locomotionClip, k_crossfadeDuration);
                } else {
                    animator.Play(0, 0, 0f);
                }
            }
        };
        timer.Start();
    }

    public float GetAnimationLength(int hash) {
        if (animator == null || animator.runtimeAnimatorController == null) return -1f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips) {
            if (Animator.StringToHash(clip.name) == hash) {
                return clip.length;
            }
        }

        return -1f;
    }

    protected abstract void SetLocomotionClip();
    protected abstract void SetAttackClip();
    protected abstract void SetSpeedHash();
}