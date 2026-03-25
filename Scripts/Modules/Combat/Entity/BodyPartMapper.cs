using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Маппер для определения части тела по названию коллайдера.
/// Поддерживает различные варианты названий для разных ригов.
/// </summary>
public static class BodyPartMapper
{
    // Хэшмапы для сопоставления ключевых слов в названиях коллайдеров с частями тела
    private static readonly Dictionary<string, BodyPart> bodyPartMap = new Dictionary<string, BodyPart>
    {
        // Голова
        { "head", BodyPart.Head },
        { "skull", BodyPart.Head },
        { "cranium", BodyPart.Head },
        
        // Шея
        { "neck", BodyPart.Neck },
        { "cervical", BodyPart.Neck },
        
        // Туловище
        { "torso", BodyPart.Torso },
        { "chest", BodyPart.Torso },
        { "spine", BodyPart.Torso },
        { "ribcage", BodyPart.Torso },
        { "back", BodyPart.Torso },
        { "abdomen", BodyPart.Torso },
        { "stomach", BodyPart.Torso },
        { "body", BodyPart.Torso },
        
        // Таз
        { "pelvis", BodyPart.Pelvis },
        { "hips", BodyPart.Pelvis },
        { "hip", BodyPart.Pelvis },
        
        // Руки - Left
        { "leftupperarm", BodyPart.LeftArm },
        { "left_upper_arm", BodyPart.LeftArm },
        { "l_upperarm", BodyPart.LeftArm },
        { "l_upper_arm", BodyPart.LeftArm },
        { "leftarm", BodyPart.LeftArm },
        { "left_arm", BodyPart.LeftArm },
        { "l_arm", BodyPart.LeftArm },
        { "larm", BodyPart.LeftArm },
        
        { "leftforearm", BodyPart.LeftArm },
        { "left_forearm", BodyPart.LeftArm },
        { "l_forearm", BodyPart.LeftArm },
        { "lforearm", BodyPart.LeftArm },
        
        { "lefthand", BodyPart.LeftHand },
        { "left_hand", BodyPart.LeftHand },
        { "l_hand", BodyPart.LeftHand },
        { "lhand", BodyPart.LeftHand },
        
        // Руки - Right
        { "rightupperarm", BodyPart.RightArm },
        { "right_upper_arm", BodyPart.RightArm },
        { "r_upperarm", BodyPart.RightArm },
        { "r_upper_arm", BodyPart.RightArm },
        { "rightarm", BodyPart.RightArm },
        { "right_arm", BodyPart.RightArm },
        { "r_arm", BodyPart.RightArm },
        { "rarm", BodyPart.RightArm },
        
        { "rightforearm", BodyPart.RightArm },
        { "right_forearm", BodyPart.RightArm },
        { "r_forearm", BodyPart.RightArm },
        { "rforearm", BodyPart.RightArm },
        
        { "righthand", BodyPart.RightHand },
        { "right_hand", BodyPart.RightHand },
        { "r_hand", BodyPart.RightHand },
        { "rhand", BodyPart.RightHand },
        
        // Ноги - Left
        { "leftthigh", BodyPart.LeftLeg },
        { "left_thigh", BodyPart.LeftLeg },
        { "l_thigh", BodyPart.LeftLeg },
        { "lthigh", BodyPart.LeftLeg },
        { "leftupperleg", BodyPart.LeftLeg },
        { "left_upper_leg", BodyPart.LeftLeg },
        { "l_upperleg", BodyPart.LeftLeg },
        { "lupperleg", BodyPart.LeftLeg },
        { "leftleg", BodyPart.LeftLeg },
        { "left_leg", BodyPart.LeftLeg },
        { "l_leg", BodyPart.LeftLeg },
        { "lleg", BodyPart.LeftLeg },
        
        { "leftcalf", BodyPart.LeftLeg },
        { "left_calf", BodyPart.LeftLeg },
        { "l_calf", BodyPart.LeftLeg },
        { "lcalf", BodyPart.LeftLeg },
        { "leftlowerleg", BodyPart.LeftLeg },
        { "left_lower_leg", BodyPart.LeftLeg },
        { "l_lowerleg", BodyPart.LeftLeg },
        { "llowerleg", BodyPart.LeftLeg },
        
        { "leftfoot", BodyPart.LeftFoot },
        { "left_foot", BodyPart.LeftFoot },
        { "l_foot", BodyPart.LeftFoot },
        { "lfoot", BodyPart.LeftFoot },
        
        // Ноги - Right
        { "rightthigh", BodyPart.RightLeg },
        { "right_thigh", BodyPart.RightLeg },
        { "r_thigh", BodyPart.RightLeg },
        { "rthigh", BodyPart.RightLeg },
        { "rightupperleg", BodyPart.RightLeg },
        { "right_upper_leg", BodyPart.RightLeg },
        { "r_upperleg", BodyPart.RightLeg },
        { "rupperleg", BodyPart.RightLeg },
        { "rightleg", BodyPart.RightLeg },
        { "right_leg", BodyPart.RightLeg },
        { "r_leg", BodyPart.RightLeg },
        { "rleg", BodyPart.RightLeg },
        
        { "rightcalf", BodyPart.RightLeg },
        { "right_calf", BodyPart.RightLeg },
        { "r_calf", BodyPart.RightLeg },
        { "rcalf", BodyPart.RightLeg },
        { "rightlowerleg", BodyPart.RightLeg },
        { "right_lower_leg", BodyPart.RightLeg },
        { "r_lowerleg", BodyPart.RightLeg },
        { "rlowerleg", BodyPart.RightLeg },
        
        { "rightfoot", BodyPart.RightFoot },
        { "right_foot", BodyPart.RightFoot },
        { "r_foot", BodyPart.RightFoot },
        { "rfoot", BodyPart.RightFoot }
    };
    
    /// <summary>
    /// Определяет часть тела по названию коллайдера
    /// </summary>
    /// <param name="colliderName">Название коллайдера (или GameObject с коллайдером)</param>
    /// <returns>Определенная часть тела или BodyPart.Unknown если не найдено</returns>
    public static BodyPart GetBodyPart(string colliderName)
    {
        if (string.IsNullOrEmpty(colliderName))
            return BodyPart.Unknown;
        
        string normalizedName = colliderName.ToLowerInvariant().Replace(" ", "").Replace("_", "");
        
        if (bodyPartMap.ContainsKey(normalizedName))
        {
            return bodyPartMap[normalizedName];
        }
        
        foreach (var kvp in bodyPartMap)
        {
            if (normalizedName.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }
        
        return BodyPart.Unknown;
    }
    
    /// <summary>
    /// Определяет часть тела по коллайдеру
    /// </summary>
    public static BodyPart GetBodyPart(Collider collider)
    {
        if (collider == null) return BodyPart.Unknown;
        return GetBodyPart(collider.name);
    }
    
    /// <summary>
    /// Определяет часть тела по GameObject
    /// </summary>
    public static BodyPart GetBodyPart(GameObject gameObject)
    {
        if (gameObject == null) return BodyPart.Unknown;
        return GetBodyPart(gameObject.name);
    }
}






