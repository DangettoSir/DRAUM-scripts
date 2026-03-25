using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Settings")]
    public Button playButton;         
    public Image fadeImage;

    [Header("Audio Settings")]
    public AudioSource musicSource;

    [Header("Vignette Settings")]
    public Volume globalVolume;
    public float vignetteStart = 1f;
    public float vignetteEnd = 0.3f;
    public float vignetteDuration = 2f;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    private Vignette vignette;

    private void Awake()
    {

        if (globalVolume != null && globalVolume.profile.TryGet<Vignette>(out var v))
        {
            vignette = v;
        }


        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);


        if (fadeImage != null)
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1f);
    }

    private void Start()
    {

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {

        float elapsed = 0f;

        float initialAlpha = fadeImage != null ? fadeImage.color.a : 0f;
        float targetAlpha = 0f;

        float startVignette = vignetteStart;
        float endVignette = vignetteEnd;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);


            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = Mathf.Lerp(initialAlpha, targetAlpha, t);
                fadeImage.color = c;
            }


            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(startVignette, endVignette, t);
            }

            yield return null;
        }


        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = targetAlpha;
            fadeImage.color = c;
        }

        if (vignette != null)
            vignette.intensity.value = endVignette;


        if (musicSource != null)
            musicSource.Play();
    }

    private void OnPlayClicked()
    {

        SceneManager.LoadScene("SampleScene");
    }
}
