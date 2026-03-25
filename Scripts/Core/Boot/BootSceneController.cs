using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class BootSceneController : MonoBehaviour
{
    [Header("Logo Settings")]
    public CanvasGroup logoGroup;
    public Color logoColor = Color.white;

    [Header("Timing Settings")]
    public float fadeInDuration = 2f;
    public float waitTime = 2f;
    public float fadeOutDuration = 2f;

    [Header("Scene Settings")]
    public string nextScene = "MainMenu";

    [Header("Audio Settings")]
    public AudioSource bootSound;

    private void Start()
    {
        if (logoGroup != null && logoGroup.GetComponent<UnityEngine.UI.Image>())
        {
            var img = logoGroup.GetComponent<UnityEngine.UI.Image>();
            img.color = logoColor;
        }

        if (!Application.isPlaying) return;

        if (bootSound != null)
            bootSound.Play();

        StartCoroutine(PlayLogo());
    }

    private System.Collections.IEnumerator PlayLogo()
    {
        yield return Fade(0f, 1f, fadeInDuration);

        yield return new WaitForSecondsRealtime(waitTime);

        yield return Fade(1f, 0f, fadeOutDuration);

        SceneManager.LoadScene(nextScene);
    }

    private System.Collections.IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (logoGroup != null)
                logoGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        if (logoGroup != null)
            logoGroup.alpha = to;
    }
}
