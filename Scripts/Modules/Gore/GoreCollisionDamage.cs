using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoreCollisionDamage : MonoBehaviour
{
    [Header("Слой цели (с SkinGoreRenderer)")]
    public LayerMask goreLayer;

    [Header("Параметры урона от скорости")]
    public float minDamage = 0.5f;
    public float maxDamage = 5f;
    public float minRadius = 0.05f;
    public float maxRadius = 0.2f;

    [Tooltip("Минимальная скорость для нанесения урона")]
    public float speedThreshold = 0.5f;

    [Header("Порог скорости для включения")]
    public float deformActivationSpeed = 1.0f;
    public float ragdollActivationSpeed = 2.0f;

    private Vector3 lastPosition;
    private Vector3 velocity;

    private void FixedUpdate()
    {
        velocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float speed = velocity.magnitude;

        if (speed < speedThreshold)
            return;

        float t = Mathf.InverseLerp(speedThreshold, 10f, speed);
        float damageAmount = Mathf.Lerp(minDamage, maxDamage, t);
        float damageRadius = Mathf.Lerp(minRadius, maxRadius, t);

        foreach (ContactPoint contact in collision.contacts)
        {
            GameObject hitObj = contact.otherCollider.gameObject;

            if (((1 << hitObj.layer) & goreLayer) != 0)
            {

                var gore = FindSkinGoreRenderer(contact.otherCollider);
                if (gore != null)
                {

                    gore.AddDamage(contact.point, damageRadius, damageAmount);
                    Debug.DrawRay(contact.point, Vector3.up * 0.2f, Color.red, 1f);
                    Debug.Log($"💥 Урон! Скорость: {speed:F2}, Урон: {damageAmount:F2}, Радиус: {damageRadius:F3}");
                }


                /*  MeshDeformer ДЕФОРМИРУЕТ ШТУКИ
                if (speed >= deformActivationSpeed)
                {
                    var deform = hitObj.GetComponentInParent<MeshDeformer>();
                    if (deform != null)
                    {
                        deform.AddDeform(contact.point);
                        Debug.Log($"🧠 Деформация при скорости {speed:F2}");
                    }
                }
                */
            }
        }
    }
    
    /// <summary>
    /// Ищет SkinGoreRenderer на объекте коллайдера или в иерархии (логика из GoreClickDemo)
    /// </summary>
    private SkinGoreRenderer FindSkinGoreRenderer(Collider collider)
    {
        if (collider == null) return null;
        
        SkinGoreRenderer r = collider.GetComponent<SkinGoreRenderer>();
        if (r != null) return r;

        Transform current = collider.transform;
        Transform rootTransform = null;
        int depth = 0;
        
        while (current != null && depth < 50)
        {
            r = current.GetComponent<SkinGoreRenderer>();
            if (r != null) return r;
            
            if (current.parent == null) rootTransform = current;
            current = current.parent;
            depth++;
        }

        if (rootTransform != null)
        {
            string rootName = rootTransform.name;
            bool isRagdoll = rootName.ToLower().Contains("ragdoll") || rootName.ToLower().Contains("variant-ragdoll");
            
            if (isRagdoll)
            {
                SkinGoreRenderer mainCharacterGore = FindMainCharacterSkinGoreRenderer(rootName);
                if (mainCharacterGore != null) return mainCharacterGore;
            }
        }
        
        current = collider.transform;
        depth = 0;
        while (current != null && depth < 50)
        {
            SkinnedMeshRenderer[] skinnedMeshes = current.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMesh in skinnedMeshes)
            {
                r = skinnedMesh.GetComponent<SkinGoreRenderer>();
                if (r != null) return r;
            }
            current = current.parent;
            depth++;
        }
        
        if (collider.attachedRigidbody != null)
        {
            current = collider.attachedRigidbody.transform;
            depth = 0;
            while (current != null && depth < 50)
            {
                r = current.GetComponent<SkinGoreRenderer>();
                if (r != null) return r;
                current = current.parent;
                depth++;
            }
        }
        
        return collider.GetComponentInParent<SkinGoreRenderer>(true);
    }
    
    /// <summary>
    /// Находит основной объект персонажа (без ragdoll) по имени ragdoll объекта
    /// </summary>
    private SkinGoreRenderer FindMainCharacterSkinGoreRenderer(string ragdollRootName)
    {
        string baseName = ragdollRootName;
        baseName = baseName.Replace("Ragdoll", "", System.StringComparison.OrdinalIgnoreCase);
        baseName = baseName.Replace("Variant-Ragdoll", "", System.StringComparison.OrdinalIgnoreCase);
        baseName = baseName.Replace("Variant", "", System.StringComparison.OrdinalIgnoreCase);
        baseName = baseName.Replace("(1)", "").Replace("(2)", "").Replace("(3)", "").Replace("(4)", "").Replace("(5)", "");
        while (baseName.Contains("  ")) baseName = baseName.Replace("  ", " ");
        baseName = baseName.Trim();
        
        SkinGoreRenderer[] allGoreRenderers = FindObjectsByType<SkinGoreRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (var gore in allGoreRenderers)
        {
            string goreObjectName = gore.gameObject.name;
            if (goreObjectName.ToLower().Contains("ragdoll")) continue;
            if (goreObjectName.Equals(baseName, System.StringComparison.OrdinalIgnoreCase)) return gore;
            if (goreObjectName.Contains(baseName, System.StringComparison.OrdinalIgnoreCase) || baseName.Contains(goreObjectName, System.StringComparison.OrdinalIgnoreCase)) return gore;
                        }
        
        foreach (var gore in allGoreRenderers)
        {
            string fullPath = GetFullPath(gore.transform);
            if (!fullPath.ToLower().Contains("ragdoll")) return gore;
        }
        
        return null;
    }
    
    private string GetFullPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
