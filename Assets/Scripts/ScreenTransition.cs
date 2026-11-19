using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScreenTransition : MonoBehaviour
{
    private static ScreenTransition instance;
    private CanvasGroup curtain;
    private bool busy;
    private string pendingScene;
    private EventSystem navigationOwner;
    private bool navigationWasEnabled;
    public static bool IsBusy => instance != null && instance.busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var transition = EnsureInstance();
        transition.StartCoroutine(transition.Run(null, true));
    }

    private static ScreenTransition EnsureInstance()
    {
        if (instance != null) return instance;
        var root = new GameObject("Screen Transitions", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        instance = root.AddComponent<ScreenTransition>();
        instance.curtain = root.GetComponent<CanvasGroup>();
        var cover = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        cover.SetParent(root.transform, false);
        cover.anchorMin = Vector2.zero; cover.anchorMax = Vector2.one;
        cover.offsetMin = cover.offsetMax = Vector2.zero;
        cover.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 1);
        instance.curtain.alpha = 0;
        instance.curtain.blocksRaycasts = false;
        return instance;
    }

    public static void Change(Action change)
    {
        var transition = EnsureInstance();
        if (transition.busy)
        {
            // Start and story callbacks can prepare their next view under the opaque curtain.
            if (transition.curtain.alpha >= 0.999f) change?.Invoke();
            return;
        }
        transition.StartCoroutine(transition.Run(change, false));
    }

    public static void LoadScene(string scene)
    {
        var transition = EnsureInstance();
        if (transition.busy)
        {
            if (transition.curtain.alpha >= 0.999f && transition.pendingScene == null) transition.pendingScene = scene;
            return;
        }
        transition.pendingScene = scene;
        transition.StartCoroutine(transition.Run(null, false));
    }

    private IEnumerator Run(Action change, bool initial)
    {
        busy = true;
        curtain.blocksRaycasts = true;
        BlockNavigation();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (initial) curtain.alpha = 1;
        else yield return Fade(1, 0.28f);
        change?.Invoke();
        // Give scene Start and story coroutines time to build their UI before uncovering it.
        for (int frame = 0; frame < 3; frame++) yield return null;
        while (pendingScene != null)
        {
            string scene = pendingScene;
            pendingScene = null;
            yield return SceneManager.LoadSceneAsync(scene);
            for (int frame = 0; frame < 3; frame++) yield return null;
        }
        Canvas.ForceUpdateCanvases();
        yield return Fade(0, 0.4f);
        curtain.blocksRaycasts = false;
        busy = false;
        RestoreNavigation();
    }

    private void Update() { if (busy) BlockNavigation(); }
    private void BlockNavigation()
    {
        if (navigationOwner == EventSystem.current) return;
        RestoreNavigation();
        navigationOwner = EventSystem.current;
        if (navigationOwner == null) return;
        navigationWasEnabled = navigationOwner.sendNavigationEvents;
        navigationOwner.sendNavigationEvents = false;
    }
    private void RestoreNavigation()
    {
        if (navigationOwner != null) navigationOwner.sendNavigationEvents = navigationWasEnabled;
        navigationOwner = null;
    }
    private void OnDestroy() => RestoreNavigation();

    private IEnumerator Fade(float target, float duration)
    {
        float from = curtain.alpha;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            curtain.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, elapsed / duration));
            yield return null;
        }
        curtain.alpha = target;
    }

    public static void Replace(RectTransform previous, RectTransform next, bool destroyPrevious = true)
        => EnsureInstance().StartCoroutine(Crossfade(previous, next, destroyPrevious));

    private static IEnumerator Crossfade(RectTransform previous, RectTransform next, bool destroyPrevious)
    {
        var incoming = next.GetComponent<CanvasGroup>();
        if (incoming == null) incoming = next.gameObject.AddComponent<CanvasGroup>();
        incoming.alpha = 0;
        incoming.interactable = false;
        incoming.blocksRaycasts = true;
        CanvasGroup outgoing = null;
        if (previous != null)
        {
            outgoing = previous.GetComponent<CanvasGroup>();
            if (outgoing == null) outgoing = previous.gameObject.AddComponent<CanvasGroup>();
            outgoing.interactable = false;
            outgoing.blocksRaycasts = false;
        }
        float oldAlpha = outgoing != null ? outgoing.alpha : 0;
        for (float elapsed = 0; elapsed < 0.22f; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / 0.22f);
            if (incoming == null) yield break;
            incoming.alpha = t;
            if (outgoing != null) outgoing.alpha = oldAlpha * (1 - t);
            yield return null;
        }
        if (incoming != null) { incoming.alpha = 1; incoming.interactable = true; }
        if (previous != null)
        {
            if (destroyPrevious) Destroy(previous.gameObject);
            else previous.gameObject.SetActive(false);
        }
    }
}
