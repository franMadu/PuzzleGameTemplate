using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PuzzleUI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueController : MonoBehaviour
{
    [SerializeField, Range(0, 120)] private float charactersPerSecond = 42;
    [SerializeField, Range(0, 1)] private float dialogueOpacity = 0.78f;
    [SerializeField, Range(0.75f, 1)] private float inactiveCharacterScale = 0.92f;
    [SerializeField, Min(0)] private float focusTransitionDuration = 0.18f;
    [SerializeField, Min(0)] private float characterTransitionDuration = 0.28f;
    [Tooltip("Máximo de líneas de texto por página de diálogo. Los textos más largos continúan en la página siguiente.")]
    [SerializeField, Min(1)] private int maxLinesPerPage = 4;
    public bool IsPlaying { get; private set; }
    public bool IsTyping { get; private set; }
    public bool IsDialogueHidden { get; private set; }
    public int PageIndex { get; private set; }
    public int PageCount => pages.Count;
    public bool IsTransitioning => Replacing(leftFocus) || Replacing(rightFocus);
    public event Action<bool> ActiveChanged;

    private sealed class Page
    {
        public Texture2D background;
        public DialogueMusicAction musicAction;
        public AudioClip music;
        public AudioClip soundEffect;
        public float soundEffectVolume;
        public string speaker;
        public string text;
        public bool isThought;
        public DialogueSpeakerSide speakerSide;
        public Sprite leftCharacter;
        public Sprite rightCharacter;
        public DialogueCharacter leftActor;
        public DialogueCharacter rightActor;
        public bool leftFacesLeft;
        public bool rightFacesLeft;
    }

    private enum Motion { None, Focus, Exit, Enter }

    private sealed class CharacterFocus
    {
        public Color fromColor;
        public Color toColor;
        public float fromScale;
        public float toScale;
        public float elapsed;
        public float duration;
        public Vector2 fromPosition;
        public Vector2 toPosition;
        public Vector2 basePosition;
        public Motion motion;
        public DialogueCharacter actor;
        public DialogueCharacter pendingActor;
        public Sprite pendingSprite;
        public Color targetColor;
        public float targetScale;
        public bool mirrored;
        public bool pendingMirror;
    }

    private readonly List<Page> pages = new List<Page>();
    [SerializeField] private RectTransform screen;
    [SerializeField, HideInInspector] private int visualStyleVersion;
    private bool uiBound;
    private Text speakerText;
    private Text bodyText;
    private Button advanceButton;
    private Button skipButton;
    private RectTransform dialogueBox;
    private GameObject nextIcon;
    private Button restoreButton;
    private RectTransform speakerTab;
    private RawImage backdrop;
    private Image leftCharacter;
    private Image rightCharacter;
    private readonly CharacterFocus leftFocus = new CharacterFocus();
    private readonly CharacterFocus rightFocus = new CharacterFocus();
    private Action onFinished;
    private GameObject previousSelection;
    private int[] textElements;
    private int visibleElements;
    private float revealProgress;
    private bool smoothPlayback;
    private bool finishing;
    private float TextSpeed => GamePreferences.CharactersPerSecond(charactersPerSecond);

    public void PlaySmooth(DialogueSequence sequence, Texture2D image = null, Action finished = null)
        => ScreenTransition.Change(() => { if (this != null) Play(sequence, image, finished, true); });

    public void Play(DialogueSequence sequence, Texture2D image = null, Action finished = null, bool smoothTransitions = false)
    {
        EnsureUI();
        var panel = MenuControls.Get<Image>(screen, "Content/Dialogue Box");
        var panelColor = panel.color;
        panelColor.a = GamePreferences.DialogueOpacity(dialogueOpacity);
        panel.color = panelColor;
        Cancel();
        smoothPlayback = smoothTransitions;
        finishing = false;
        GameAudio.PlayMusic(sequence != null ? sequence.Music : null, this);
        screen.gameObject.SetActive(true);
        SetDialogueHidden(false);
        Canvas.ForceUpdateCanvases();
        pages.Clear();
        if (sequence != null)
        {
            if (!sequence.ValidateAppearances(out string appearanceError)) Debug.LogWarning(appearanceError, sequence);
            var stages = DialogueStagePlanner.Build(sequence);
            if (stages.Count > 0)
            {
                foreach (var stage in stages)
                    AddPages(new Page {
                        background = stage.Line.background, soundEffect = stage.Line.soundEffect,
                        musicAction = stage.Line.musicAction, music = stage.Line.music,
                        soundEffectVolume = stage.Line.soundEffectVolume,
                        speaker = stage.Speaker != null ? stage.Speaker.DisplayName : stage.Line.speaker,
                        text = stage.Line.text, isThought = stage.Line.isThought, speakerSide = stage.SpeakingSide,
                        leftActor = stage.Left, rightActor = stage.Right,
                        leftCharacter = stage.Line.hideLeftCharacter ? null : stage.Line.leftCharacter != null ? stage.Line.leftCharacter : stage.LeftAppearance.Portrait,
                        rightCharacter = stage.Line.hideRightCharacter ? null : stage.Line.rightCharacter != null ? stage.Line.rightCharacter : stage.RightAppearance.Portrait,
                        leftFacesLeft = stage.LeftAppearance.FacesLeft,
                        rightFacesLeft = stage.RightAppearance.FacesLeft
                    });
            }
            else
            {
                Sprite left = sequence.LeftCharacter;
                Sprite right = sequence.RightCharacter;
                foreach (var line in sequence.Lines)
                {
                    if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
                    if (line.hideLeftCharacter) left = null;
                    else if (line.leftCharacter != null) left = line.leftCharacter;
                    if (line.hideRightCharacter) right = null;
                    else if (line.rightCharacter != null) right = line.rightCharacter;
                    AddPages(new Page { speaker = line.speaker, text = line.text, isThought = line.isThought, speakerSide = line.speakerSide,
                        background = line.background, soundEffect = line.soundEffect, soundEffectVolume = line.soundEffectVolume,
                        musicAction = line.musicAction, music = line.music,
                        leftCharacter = left, rightCharacter = right, leftFacesLeft = sequence.LeftFacesLeft, rightFacesLeft = sequence.RightFacesLeft });
                }
            }
        }
        if (pages.Count == 0)
        {
            GameAudio.StopMusic(this);
            screen.gameObject.SetActive(false);
            finished?.Invoke();
            return;
        }
        onFinished = finished;
        if (sequence != null && sequence.Background != null) image = sequence.Background;
        SetBackground(image);
        if (EventSystem.current != null)
        {
            previousSelection = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null);
        }
        PageIndex = 0;
        IsPlaying = true;
        screen.gameObject.SetActive(true);
        screen.SetAsLastSibling();
        ShowPage(true);
        ActiveChanged?.Invoke(true);
    }

    private void SetBackground(Texture2D image)
    {
        backdrop.texture = image;
        backdrop.gameObject.SetActive(image != null);
        if (image != null)
        {
            float imageAspect = image.width / (float)image.height;
            float screenAspect = screen.rect.width / Mathf.Max(1, screen.rect.height);
            float width = Mathf.Min(1, screenAspect / imageAspect);
            float height = Mathf.Min(1, imageAspect / screenAspect);
            backdrop.uvRect = new Rect((1 - width) / 2, (1 - height) / 2, width, height);
        }
    }

    public void Advance()
    {
        if (!IsPlaying || finishing || ScreenTransition.IsBusy) return;
        if (IsDialogueHidden) { RestoreDialogue(); return; }
        if (IsTransitioning) return;
        if (IsTyping)
        {
            Reveal(textElements.Length);
            return;
        }
        if (PageIndex + 1 >= pages.Count) Finish(true);
        else { PageIndex++; ShowPage(); }
    }

    public void Skip() => Finish(true);
    public void Cancel() => Finish(false);

    private void ReturnToMenu()
    {
        if (!IsPlaying || ScreenTransition.IsBusy) return;
        // Leaving a conversation never invokes the completion callback or advances the story.
        onFinished = null;
        PuzzleSession.StoryMode = false;
        PuzzleSession.EndReplay();
        PuzzleSession.MenuPage = PuzzleMenuPage.Main;
        PuzzleSession.TitleDismissed = true;
        ScreenTransition.Change(() =>
        {
            if (this != null) Cancel();
            ScreenTransition.LoadScene(PuzzleSession.MenuScene);
        });
    }

    public void HideDialogue()
    {
        if (!IsPlaying || finishing || ScreenTransition.IsBusy) return;
        SetDialogueHidden(true);
    }

    public void RestoreDialogue()
    {
        if (!IsPlaying || !IsDialogueHidden || finishing || ScreenTransition.IsBusy) return;
        SetDialogueHidden(false);
    }

    private void SetDialogueHidden(bool hidden)
    {
        IsDialogueHidden = hidden;
        dialogueBox.gameObject.SetActive(!hidden);
        restoreButton.gameObject.SetActive(hidden);
        if (hidden && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void UpdateAdvanceControl()
    {
        advanceButton.interactable = !IsTransitioning;
        nextIcon.SetActive(!IsTyping && !IsTransitioning);
    }

    private void OnDisable() => Cancel();

    private void Update()
    {
        if (!IsPlaying || finishing || ScreenTransition.IsBusy) return;
        UpdateFocus(leftCharacter, leftFocus);
        UpdateFocus(rightCharacter, rightFocus);
        UpdateAdvanceControl();
        if (!IsDialogueHidden && !IsTransitioning && IsTyping && TextSpeed > 0)
        {
            revealProgress += Time.unscaledDeltaTime * TextSpeed;
            Reveal(Mathf.Min(textElements.Length, (int)revealProgress));
        }
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (IsDialogueHidden)
        {
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) RestoreDialogue();
            return;
        }
        if (keyboard.escapeKey.wasPressedThisFrame) Skip();
        else if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Advance();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (IsDialogueHidden)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) RestoreDialogue();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) Skip();
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Advance();
#endif
    }

    private void ShowPage(bool snapFocus = false)
    {
        var page = pages[PageIndex];
        if (page.background != null) SetBackground(page.background);
        if (page.musicAction == DialogueMusicAction.Play && page.music != null) GameAudio.PlayMusic(page.music, this);
        else if (page.musicAction == DialogueMusicAction.Stop) GameAudio.PlayMusic(null, this);
        GameAudio.PlayDialogueEffect(page.soundEffect, page.soundEffectVolume, this);
        var style = bodyText.GetComponent<DialogueTextStyle>();
        if (style != null) style.Apply(page.isThought);
        speakerText.text = page.speaker;
        speakerTab.gameObject.SetActive(!string.IsNullOrWhiteSpace(page.speaker));
        SetCharacter(leftCharacter, page.leftCharacter, page.leftActor, page.leftFacesLeft,
            page.speakerSide == DialogueSpeakerSide.Left, false, leftFocus, snapFocus);
        SetCharacter(rightCharacter, page.rightCharacter, page.rightActor, page.rightFacesLeft,
            page.speakerSide == DialogueSpeakerSide.Right, true, rightFocus, snapFocus);
        advanceButton.interactable = !IsTransitioning;
        bool rightSpeaker = page.speakerSide == DialogueSpeakerSide.Right;
        speakerTab.anchorMin = speakerTab.anchorMax = new Vector2(rightSpeaker ? 1 : 0, 1);
        speakerTab.pivot = new Vector2(rightSpeaker ? 1 : 0, 0);
        speakerTab.anchoredPosition = Vector2.zero;
        textElements = StringInfo.ParseCombiningCharacters(page.text);
        visibleElements = -1;
        revealProgress = 0;
        Reveal(TextSpeed <= 0 ? textElements.Length : 0);
    }

    private void Reveal(int count)
    {
        if (visibleElements == count) return;
        visibleElements = count;
        IsTyping = count < textElements.Length;
        string text = pages[PageIndex].text;
        bodyText.text = IsTyping ? text.Substring(0, textElements[count]) : text;
        UpdateAdvanceControl();
    }

    private void Finish(bool invokeCallback)
    {
        if (!IsPlaying) return;
        if (invokeCallback && smoothPlayback)
        {
            if (finishing || ScreenTransition.IsBusy) return;
            finishing = true;
            ScreenTransition.Change(() =>
            {
                smoothPlayback = false;
                Finish(true);
            });
            return;
        }
        finishing = false;
        IsPlaying = false;
        GameAudio.StopMusic(this);
        GameAudio.StopDialogueEffects(this);
        IsTyping = false;
        SetDialogueHidden(false);
        leftFocus.motion = rightFocus.motion = Motion.None;
        var callback = onFinished;
        onFinished = null;
        screen.gameObject.SetActive(false);
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        previousSelection = null;
        ActiveChanged?.Invoke(false);
        if (invokeCallback) callback?.Invoke();
    }

    private static bool Replacing(CharacterFocus focus) => focus.motion == Motion.Exit || focus.motion == Motion.Enter;

    private void SetCharacter(Image image, Sprite sprite, DialogueCharacter actor, bool facesLeft,
        bool speaking, bool rightSlot, CharacterFocus focus, bool snap)
    {
        bool sameVisibleActor = actor != null && focus.actor == actor && image.sprite != null && sprite != null;
        bool occupantChanged = focus.actor != actor || (!sameVisibleActor && image.sprite != sprite);
        focus.pendingSprite = sprite;
        focus.pendingActor = actor;
        focus.pendingMirror = rightSlot != facesLeft;
        focus.targetColor = speaking ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1);
        focus.targetScale = speaking ? 1 : inactiveCharacterScale;
        if (snap)
        {
            AssignPending(image, focus);
            Pose(image, focus, focus.targetColor, focus.targetScale, focus.basePosition);
            focus.motion = Motion.None;
        }
        else if (occupantChanged || (!image.gameObject.activeSelf && sprite != null))
        {
            if (image.gameObject.activeSelf && image.sprite != null)
            {
                float scale = Mathf.Abs(image.rectTransform.localScale.x);
                BeginMotion(image, focus, Motion.Exit, image.color, scale,
                    OffscreenPosition(image, focus, scale), characterTransitionDuration);
            }
            else BeginArrival(image, focus);
        }
        else
        {
            AssignPending(image, focus);
            BeginMotion(image, focus, Motion.Focus, focus.targetColor, focus.targetScale, focus.basePosition, focusTransitionDuration);
        }
    }

    private static void Pose(Image image, CharacterFocus focus, Color color, float scale, Vector2 position)
    {
        image.color = color;
        image.rectTransform.localScale = new Vector3(focus.mirrored ? -scale : scale, scale, 1);
        image.rectTransform.anchoredPosition = position;
    }

    private static void AssignPending(Image image, CharacterFocus focus)
    {
        image.sprite = focus.pendingSprite;
        if (focus.pendingSprite != null)
        {
            // Fit portraits to the authored height. A fixed width makes wider sprites look shorter.
            var spriteRect = focus.pendingSprite.rect;
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                image.rectTransform.rect.height * spriteRect.width / spriteRect.height);
        }
        focus.actor = focus.pendingActor;
        focus.mirrored = focus.pendingMirror;
        image.gameObject.SetActive(focus.pendingSprite != null);
    }

    private void BeginArrival(Image image, CharacterFocus focus)
    {
        AssignPending(image, focus);
        if (focus.pendingSprite == null) { focus.motion = Motion.None; return; }
        Pose(image, focus, focus.targetColor, focus.targetScale, OffscreenPosition(image, focus, focus.targetScale));
        BeginMotion(image, focus, Motion.Enter, focus.targetColor, focus.targetScale, focus.basePosition, characterTransitionDuration);
    }

    private static Vector2 OffscreenPosition(Image image, CharacterFocus focus, float scale)
    {
        // Each portrait is anchored to its own screen edge, with a centered horizontal pivot.
        float distance = image.rectTransform.rect.width * scale * 0.5f + 24;
        return new Vector2(image.rectTransform.anchorMin.x > 0.5f ? distance : -distance, focus.basePosition.y);
    }

    private void BeginMotion(Image image, CharacterFocus focus, Motion motion, Color color, float scale, Vector2 position, float duration)
    {
        focus.fromColor = image.color;
        focus.toColor = color;
        focus.fromScale = Mathf.Abs(image.rectTransform.localScale.x);
        focus.toScale = scale;
        focus.fromPosition = image.rectTransform.anchoredPosition;
        focus.toPosition = position;
        focus.duration = duration;
        focus.elapsed = 0;
        focus.motion = motion;
        if (duration <= 0)
        {
            Pose(image, focus, color, scale, position);
            CompleteMotion(image, focus);
        }
    }

    private void CompleteMotion(Image image, CharacterFocus focus)
    {
        if (focus.motion == Motion.Exit) BeginArrival(image, focus);
        else focus.motion = Motion.None;
    }

    private void UpdateFocus(Image image, CharacterFocus focus)
    {
        if (focus.motion == Motion.None) return;
        focus.elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(focus.elapsed / Mathf.Max(0.001f, focus.duration));
        float smooth = Mathf.SmoothStep(0, 1, progress);
        Pose(image, focus, Color.Lerp(focus.fromColor, focus.toColor, smooth),
            Mathf.Lerp(focus.fromScale, focus.toScale, smooth), Vector2.Lerp(focus.fromPosition, focus.toPosition, smooth));
        if (progress >= 1) CompleteMotion(image, focus);
    }

    private void AddPages(Page source)
    {
        string remaining = source.text.Trim();
        bool firstPage = true;
        var settings = bodyText.GetGenerationSettings(new Vector2(bodyText.rectTransform.rect.width, 0));
        var style = bodyText.GetComponent<DialogueTextStyle>();
        if (style != null) settings.fontStyle = style.GetFontStyle(source.isThought);
        while (remaining.Length > 0)
        {
            int[] offsets = StringInfo.ParseCombiningCharacters(remaining);
            int low = 1, high = offsets.Length, end = offsets.Length > 1 ? offsets[1] : remaining.Length;
            while (low <= high)
            {
                int count = (low + high) / 2;
                int length = count < offsets.Length ? offsets[count] : remaining.Length;
                var generator = bodyText.cachedTextGeneratorForLayout;
                float height = generator.GetPreferredHeight(remaining.Substring(0, length), settings) / bodyText.pixelsPerUnit;
                if (height <= bodyText.rectTransform.rect.height - 4 && generator.lineCount <= Mathf.Max(1, maxLinesPerPage))
                { end = length; low = count + 1; }
                else high = count - 1;
            }
            if (end < remaining.Length)
            {
                int space = remaining.LastIndexOfAny(new[] { ' ', '\n', '\t' }, end - 1);
                if (space > end / 2) end = space;
            }
            pages.Add(new Page { speaker = source.speaker, speakerSide = source.speakerSide,
                background = source.background,
                musicAction = firstPage ? source.musicAction : DialogueMusicAction.Keep, music = source.music,
                soundEffect = firstPage ? source.soundEffect : null, soundEffectVolume = source.soundEffectVolume,
                isThought = source.isThought,
                leftCharacter = source.leftCharacter, rightCharacter = source.rightCharacter,
                leftActor = source.leftActor, rightActor = source.rightActor,
                leftFacesLeft = source.leftFacesLeft, rightFacesLeft = source.rightFacesLeft,
                text = remaining.Substring(0, end).TrimEnd() });
            remaining = remaining.Substring(end).TrimStart();
            firstPage = false;
        }
    }

    private void EnsureUI()
    {
        if (uiBound) return;
        backdrop = MenuControls.Get<RawImage>(screen, "Chapter Background");
        leftCharacter = MenuControls.Get<Image>(screen, "Content/Left Character");
        rightCharacter = MenuControls.Get<Image>(screen, "Content/Right Character");
        leftFocus.basePosition = leftCharacter.rectTransform.anchoredPosition;
        rightFocus.basePosition = rightCharacter.rectTransform.anchoredPosition;
        var panel = dialogueBox = MenuControls.Get<RectTransform>(screen, "Content/Dialogue Box");
        speakerTab = MenuControls.Get<RectTransform>(panel, "Speaker Name Box");
        speakerText = MenuControls.Get<Text>(speakerTab, "Name");
        bodyText = MenuControls.Get<Text>(panel, "Body");
        advanceButton = MenuControls.Bind(screen, "Content/Dialogue Box", Advance);
        nextIcon = panel.Find("Next Icon").gameObject;
        MenuControls.Bind(panel, "Hide", HideDialogue);
        restoreButton = MenuControls.Bind(screen, "Restore Dialogue", RestoreDialogue);
        skipButton = MenuControls.Bind(panel, "Skip", Skip);
        MenuControls.Bind(screen, "Back to Menu", ReturnToMenu);
        uiBound = true;
    }
}
