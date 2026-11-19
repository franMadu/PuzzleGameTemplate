using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Persistent 2D sources separate background music from interaction effects across scenes.
public class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private AudioSource source;
    private AudioClip buttonClick;
    private AudioClip tileSlide;
    private AudioClip tilePlace;
    private float nextDragSound;
    private AudioSource musicSource;
    private AudioClip targetMusic;
    private Object musicOwner;
    private Coroutine musicTransition;
    private float musicFade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        SceneManager.sceneLoaded -= BindSceneButtons;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
        SceneManager.sceneLoaded += BindSceneButtons;
    }

    private static GameAudio EnsureInstance()
    {
        if (instance != null) return instance;
        var root = new GameObject("Game Audio");
        instance = root.AddComponent<GameAudio>();
        DontDestroyOnLoad(root);
        instance.source = root.AddComponent<AudioSource>();
        instance.source.playOnAwake = false;
        instance.source.spatialBlend = 0;
        instance.source.ignoreListenerPause = true;
        instance.musicSource = root.AddComponent<AudioSource>();
        instance.musicSource.playOnAwake = false;
        instance.musicSource.spatialBlend = 0;
        instance.musicSource.loop = true;
        instance.musicSource.volume = 0;
        instance.buttonClick = Resources.Load<AudioClip>("Audio/ButtonClick");
        instance.tileSlide = Resources.Load<AudioClip>("Audio/TileSlide");
        instance.tilePlace = Resources.Load<AudioClip>("Audio/TilePlace");
        return instance;
    }

    private static void BindSceneButtons(Scene scene, LoadSceneMode mode)
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ButtonSound.Bind(button);
    }

    public static void Click()
    {
        var audio = EnsureInstance();
        audio.Play(audio.buttonClick, 0.6f);
    }

    public static void DragTile()
    {
        var audio = EnsureInstance();
        // Pointer events may arrive every frame; keep the scrape soft and evenly spaced.
        if (Time.unscaledTime < audio.nextDragSound) return;
        audio.nextDragSound = Time.unscaledTime + 0.075f;
        audio.Play(audio.tileSlide, 0.5f);
    }

    public static void PlaceTile()
    {
        var audio = EnsureInstance();
        audio.Play(audio.tilePlace, 0.55f);
    }

    public static void PlayMusic(AudioClip clip, Object owner = null)
    {
        var audio = EnsureInstance();
        audio.musicOwner = owner;
        if (audio.targetMusic == clip) return;
        audio.targetMusic = clip;
        if (audio.musicTransition != null) audio.StopCoroutine(audio.musicTransition);
        audio.musicTransition = audio.StartCoroutine(audio.ChangeMusic(clip));
    }

    public static void StopMusic(Object owner)
    {
        // Old scene callbacks must not stop a new conversation's soundtrack.
        if (instance != null && instance.isActiveAndEnabled && instance.musicOwner == owner) PlayMusic(null);
    }

    private IEnumerator ChangeMusic(AudioClip clip)
    {
        if (musicSource.clip != clip)
        {
            if (musicSource.clip != null) yield return FadeMusic(0, 0.35f);
            musicSource.Stop();
            musicSource.clip = clip;
            musicFade = 0;
            if (clip != null) musicSource.Play();
        }
        if (clip != null) yield return FadeMusic(1, 0.5f);
        musicTransition = null;
    }

    private IEnumerator FadeMusic(float target, float duration)
    {
        float from = musicFade;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            musicFade = Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, elapsed / duration));
            ApplyMusicVolume();
            yield return null;
        }
        musicFade = target;
        ApplyMusicVolume();
    }

    private void Update() => ApplyMusicVolume();
    private void ApplyMusicVolume()
    {
        if (musicSource != null) musicSource.volume = musicFade * GamePreferences.MusicVolume * 0.55f;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null) source.PlayOneShot(clip, volume * GamePreferences.EffectsVolume);
    }
}
