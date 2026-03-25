using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class EmissionPulse : MonoBehaviour
{
    public float speed = 1.5f;
    public float intensity = 2.5f;
    private Material mat;
    private Color baseEmission;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        baseEmission = mat.GetColor("_EmissionColor");
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f; // от 0 до 1
        Color newEmission = baseEmission * (1f + t * intensity);
        mat.SetColor("_EmissionColor", newEmission);
    }
}
