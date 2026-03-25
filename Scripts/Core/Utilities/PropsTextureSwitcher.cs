using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class PropsTextureSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class TextureOption
    {
        public string name;
        public Texture texture;
    }

    public TextureOption[] options;

    public int selected;

    private Renderer rend;

    private void OnValidate()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (rend != null && options != null && options.Length > 0)
        {
            selected = Mathf.Clamp(selected, 0, options.Length - 1);

            Material mat = rend.sharedMaterial;
            if (mat != null && options[selected].texture != null)
            {
                mat.mainTexture = options[selected].texture;
            }
        }
    }
}
