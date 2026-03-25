using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Player.Events;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DRAUM.Modules.Audio
{
    public class AudioModule : ServiceModuleBase
    {
        public override string ModuleName => "Audio";

        [Header("Footsteps")]
        public FootstepAudioConfig footstepConfig;
        [Range(0f, 2f)] public float footstepVolumeMultiplier = 1f;
        [Range(0f, 0.5f)] public float pitchVariation = 0.1f;

        [Header("Interaction")]
        public InteractionAudioConfig interactionConfig;
        [Range(0f, 2f)] public float interactionVolumeMultiplier = 1f;

        [Header("Combat")]
        public CombatAudioConfig combatConfig;
        [Range(0f, 2f)] public float combatVolumeMultiplier = 1f;

        [Header("UI")]
        public UIAudioConfig uiConfig;
        [Range(0f, 2f)] public float uiVolumeMultiplier = 1f;

        [Header("Debug")]
        public bool showAudioLogs = false;

        private Dictionary<string, FootstepAudioConfig.MaterialSounds> materialSoundsCache = new();
        private Dictionary<string, InteractionAudioConfig.ItemPickupSounds> itemPickupCache = new();
        private Dictionary<string, CombatAudioConfig.WeaponHitSounds> weaponHitCache = new();
        
        private List<AudioSource> pool = new();
        private const int POOL_SIZE = 16;

        protected override void OnInitialize()
        {
            pool = new List<AudioSource>();
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.maxDistance = 20f;
                src.rolloffMode = AudioRolloffMode.Linear;
                pool.Add(src);
            }

            if (footstepConfig != null)
            {
                foreach (var matSounds in footstepConfig.materialSounds)
                {
                    string key = !string.IsNullOrEmpty(matSounds.materialName) 
                        ? matSounds.materialName 
                        : (matSounds.physicsMaterial != null ? matSounds.physicsMaterial.name : null);
                    
                    if (!string.IsNullOrEmpty(key))
                    {
                        materialSoundsCache[key] = matSounds;
                    }
                }
            }

            if (interactionConfig != null)
            {
                foreach (var itemSound in interactionConfig.itemPickupSounds)
                {
                    if (!string.IsNullOrEmpty(itemSound.itemName))
                    {
                        itemPickupCache[itemSound.itemName] = itemSound;
                    }
                }
            }

            if (combatConfig != null)
            {
                foreach (var weaponSound in combatConfig.weaponHitSounds)
                {
                    if (!string.IsNullOrEmpty(weaponSound.weaponName))
                    {
                        weaponHitCache[weaponSound.weaponName] = weaponSound;
                    }
                }
            }

            Events.Subscribe<PlayerFootstepEvent>(OnPlayerFootstep);
            Events.Subscribe<PlayerInteractionEvent>(OnPlayerInteraction);
            Events.Subscribe<PlayerCombatHitEvent>(OnPlayerCombatHit);
            Events.Subscribe<CombatAnimationCueEvent>(OnCombatAnimationCue);
            Events.Subscribe<UISoundEvent>(OnUISound);
            
            DraumLogger.Info(this, "[AudioModule] Initialized.");
        }

        protected override void OnShutdown()
        {
            if (EventBus.Instance != null)
            {
                Events.Unsubscribe<PlayerFootstepEvent>(OnPlayerFootstep);
                Events.Unsubscribe<PlayerInteractionEvent>(OnPlayerInteraction);
                Events.Unsubscribe<PlayerCombatHitEvent>(OnPlayerCombatHit);
                Events.Unsubscribe<CombatAnimationCueEvent>(OnCombatAnimationCue);
                Events.Unsubscribe<UISoundEvent>(OnUISound);
            }
            
            foreach (var src in pool)
            {
                if (src != null) Destroy(src);
            }
            pool.Clear();
            materialSoundsCache.Clear();
            itemPickupCache.Clear();
            weaponHitCache.Clear();
        }

        #region Footstep Sounds

        private void OnPlayerFootstep(PlayerFootstepEvent evt)
        {
            if (footstepConfig == null) return;

            AudioClip[] clips = null;
            float volume = footstepConfig.defaultVolume * footstepVolumeMultiplier;

            if (materialSoundsCache.TryGetValue(evt.MaterialName, out var matSounds))
            {
                if (evt.IsSprinting)
                {
                    clips = matSounds.sprintClips.Length > 0 ? matSounds.sprintClips : matSounds.walkClips;
                    volume = matSounds.volume * footstepVolumeMultiplier * matSounds.sprintVolumeMultiplier;
                }
                else if (evt.IsCrouched)
                {
                    clips = matSounds.crouchClips.Length > 0 ? matSounds.crouchClips : matSounds.walkClips;
                    volume = matSounds.volume * footstepVolumeMultiplier * matSounds.crouchVolumeMultiplier;
                }
                else
                {
                    clips = matSounds.walkClips;
                    volume = matSounds.volume * footstepVolumeMultiplier;
                }
            }
            else
            {
                if (evt.IsSprinting)
                {
                    clips = footstepConfig.defaultSprintClips.Length > 0 
                        ? footstepConfig.defaultSprintClips 
                        : footstepConfig.defaultWalkClips;
                }
                else if (evt.IsCrouched)
                {
                    clips = footstepConfig.defaultCrouchClips.Length > 0 
                        ? footstepConfig.defaultCrouchClips 
                        : footstepConfig.defaultWalkClips;
                }
                else
                {
                    clips = footstepConfig.defaultWalkClips;
                }
            }

            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

            PlaySound(clip, evt.Position, volume, pitch, $"Footstep_{(evt.IsSprinting ? "Sprint" : evt.IsCrouched ? "Crouch" : "Walk")}");
        }

        #endregion

        #region Interaction Sounds

        private void OnPlayerInteraction(PlayerInteractionEvent evt)
        {
            if (interactionConfig == null) return;

            AudioClip[] clips = null;
            float volume = interactionConfig.defaultPickupVolume * interactionVolumeMultiplier;

            if (evt.InteractionType == InteractionType.Pickable && !string.IsNullOrEmpty(evt.ItemName))
            {
                if (itemPickupCache.TryGetValue(evt.ItemName, out var itemSound))
                {
                    if (itemSound.specificClips.Length > 0)
                    {
                        clips = itemSound.specificClips;
                        volume = itemSound.volume * interactionVolumeMultiplier;
                    }
                }

                if (clips == null || clips.Length == 0)
                {
                    clips = interactionConfig.defaultPickupClips;
                }
            }
            else
            {
                switch (evt.InteractionType)
                {
                    case InteractionType.Button:
                        clips = interactionConfig.buttonClips;
                        break;
                    case InteractionType.Lever:
                        clips = interactionConfig.leverClips;
                        break;
                    case InteractionType.Door:
                        clips = interactionConfig.doorClips;
                        break;
                    case InteractionType.Container:
                        clips = interactionConfig.containerClips;
                        break;
                }
                volume = interactionConfig.defaultInteractionVolume * interactionVolumeMultiplier;
            }

            if (clips == null || clips.Length == 0)
            {
                if (showAudioLogs)
                {
                    DraumLogger.Info(this, $"[AudioModule] No sounds for interaction: {evt.InteractionType}, Item: {evt.ItemName}");
                }
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlaySound(clip, evt.Position, volume, 1f, $"Interaction_{evt.InteractionType}");
        }

        #endregion

        #region Combat Sounds

        private void OnPlayerCombatHit(PlayerCombatHitEvent evt)
        {
            if (combatConfig == null) return;
            if (evt == null) return;
            if (!evt.IsImpact) return;

            AudioClip[] clips = null;
            float volume = combatConfig.defaultHitVolume * combatVolumeMultiplier;

            if ((clips == null || clips.Length == 0) && !string.IsNullOrEmpty(evt.WeaponName))
            {
                if (weaponHitCache.TryGetValue(evt.WeaponName, out var weaponSound))
                {
                    if (!string.IsNullOrEmpty(evt.MaterialName) && weaponSound.materialHits != null)
                    {
                        var materialHit = weaponSound.materialHits.FirstOrDefault(m => m.materialName == evt.MaterialName);
                        if (materialHit != null && materialHit.clips.Length > 0)
                        {
                            clips = materialHit.clips;
                            volume = materialHit.volume * combatVolumeMultiplier;
                        }
                    }

                    if (clips == null || clips.Length == 0)
                    {
                        clips = weaponSound.hitClips;
                        volume = weaponSound.volume * combatVolumeMultiplier;
                    }
                }
            }

            if ((clips == null || clips.Length == 0) && !string.IsNullOrEmpty(evt.MaterialName))
            {
                if (combatConfig.defaultMaterialHits != null)
                {
                    var defaultMaterial = combatConfig.defaultMaterialHits.FirstOrDefault(m => m.materialName == evt.MaterialName);
                    if (defaultMaterial != null && defaultMaterial.clips.Length > 0)
                    {
                        clips = defaultMaterial.clips;
                        volume = defaultMaterial.volume * combatVolumeMultiplier;
                    }
                }
            }

            if (clips == null || clips.Length == 0)
            {
                clips = combatConfig.defaultHitClips;
            }

            if (clips == null || clips.Length == 0)
            {
                if (showAudioLogs)
                {
                    DraumLogger.Info(this, $"[AudioModule] No sounds for hit: Weapon={evt.WeaponName}, Material={evt.MaterialName}, Direction={evt.SwingDirection}");
                }
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlaySound(clip, evt.HitPosition, volume, 1f, $"CombatHit_{evt.WeaponName}");
        }

        private void OnCombatAnimationCue(CombatAnimationCueEvent evt)
        {
            if (combatConfig == null || evt == null) return;
            if (string.IsNullOrEmpty(evt.CueKey)) return;
            if (!IsTargetMatch(evt.Target, "Audio")) return;

            string cue = evt.CueKey;
            bool isSwingCue = cue.Contains("Swing", System.StringComparison.OrdinalIgnoreCase) ||
                              cue.Contains("Whoosh", System.StringComparison.OrdinalIgnoreCase) ||
                              cue.Contains("Attack", System.StringComparison.OrdinalIgnoreCase);
            if (!isSwingCue) return;

            AudioClip[] clips = null;
            float volume = combatConfig.swingVolume * combatVolumeMultiplier;
            string direction = evt.SwingDirection;

            if (string.IsNullOrEmpty(direction))
            {
                if (cue.Contains("Left", System.StringComparison.OrdinalIgnoreCase)) direction = "Left";
                else if (cue.Contains("Right", System.StringComparison.OrdinalIgnoreCase)) direction = "Right";
                else if (cue.Contains("Up", System.StringComparison.OrdinalIgnoreCase)) direction = "Up";
            }

            switch (direction)
            {
                case "Left":
                    clips = combatConfig.leftSwingClips;
                    break;
                case "Right":
                    clips = combatConfig.rightSwingClips;
                    break;
                case "Up":
                    clips = combatConfig.upSwingClips;
                    break;
            }

            if (clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlaySound(clip, evt.Position, volume, 1f, $"CombatAnimCue_{cue}_{direction}");
        }

        private static bool IsTargetMatch(string target, string channel)
        {
            if (string.IsNullOrWhiteSpace(target)) return true;
            return target.Equals("All", System.StringComparison.OrdinalIgnoreCase) ||
                   target.Equals(channel, System.StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region UI Sounds

        private void OnUISound(UISoundEvent evt)
        {
            if (uiConfig == null) return;

            AudioClip clip = null;
            float volume = 0.5f * uiVolumeMultiplier;
            Vector3 position = Vector3.zero;

            switch (evt.SoundType)
            {
                case UISoundType.BackpackOpen:
                    clip = uiConfig.inventorySounds.backpackOpenClip;
                    volume = uiConfig.backpackVolume * uiVolumeMultiplier;
                    break;
                    
                case UISoundType.BackpackClose:
                    clip = uiConfig.inventorySounds.backpackCloseClip;
                    volume = uiConfig.backpackVolume * uiVolumeMultiplier;
                    break;
                    
                case UISoundType.SectionTransition:
                    if (uiConfig.inventorySounds.sectionTransitionClips != null && 
                        uiConfig.inventorySounds.sectionTransitionClips.Length > 0)
                    {
                        clip = uiConfig.inventorySounds.sectionTransitionClips[
                            Random.Range(0, uiConfig.inventorySounds.sectionTransitionClips.Length)];
                    }
                    volume = uiConfig.sectionTransitionVolume * uiVolumeMultiplier;
                    break;
                    
                case UISoundType.LeftClick:
                    if (uiConfig.inventorySounds.leftClickClips != null && 
                        uiConfig.inventorySounds.leftClickClips.Length > 0)
                    {
                        clip = uiConfig.inventorySounds.leftClickClips[
                            Random.Range(0, uiConfig.inventorySounds.leftClickClips.Length)];
                    }
                    volume = uiConfig.clickVolume * uiVolumeMultiplier;
                    break;
                    
                case UISoundType.RightClick:
                    if (uiConfig.inventorySounds.rightClickClips != null && 
                        uiConfig.inventorySounds.rightClickClips.Length > 0)
                    {
                        clip = uiConfig.inventorySounds.rightClickClips[
                            Random.Range(0, uiConfig.inventorySounds.rightClickClips.Length)];
                    }
                    volume = uiConfig.clickVolume * uiVolumeMultiplier;
                    break;
            }

            if (clip == null)
            {
                if (showAudioLogs)
                {
                    DraumLogger.Info(this, $"[AudioModule] Missing UI sound: {evt.SoundType}, Context: {evt.Context}");
                }
                return;
            }

            
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                position = mainCam.transform.position;
            }

            PlaySound(clip, position, volume, 1f, $"UI_{evt.SoundType}");
        }

        #endregion

        #region Helper Methods

        private void PlaySound(AudioClip clip, Vector3 position, float volume, float pitch, string debugName = "")
        {
            if (clip == null) return;

            var freeSrc = pool.FirstOrDefault(s => !s.isPlaying);
            if (freeSrc == null)
            {
                if (showAudioLogs)
                {
                    DraumLogger.Warning(this, $"[AudioModule] No free AudioSource for {debugName}");
                }
                return;
            }

            freeSrc.transform.position = position;
            freeSrc.clip = clip;
            freeSrc.volume = volume;
            freeSrc.pitch = pitch;
            freeSrc.Play();

            if (showAudioLogs)
            {
                DraumLogger.Info(this, $"[AudioModule] Played sound: {debugName}, Clip={clip.name}, Volume={volume:F2}, Pitch={pitch:F2}");
            }
        }

        #endregion
    }
}