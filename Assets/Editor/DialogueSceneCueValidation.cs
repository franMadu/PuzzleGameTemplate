using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DialogueSceneCueValidation
{
    private const string Key = "DialogueSceneCueValidation.Active";
    static DialogueSceneCueValidation() => EditorApplication.update += Tick;

    [MenuItem("Tools/One Hand Puzzle/Validate Dialogue Backgrounds and Sounds (Play Mode)")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EngineInteractionValidation.Run();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Story.unity");
        foreach (var story in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StorySceneController>(true)))
            story.enabled = false;
        SessionState.SetBool(Key, true);
        SessionState.SetInt(Key + ".Phase", 0);
        SessionState.SetFloat(Key + ".Deadline", (float)EditorApplication.timeSinceStartup + 90);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        int phase = SessionState.GetInt(Key + ".Phase", 0);
        if (phase != 0)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(Key, false);
            if (Application.isBatchMode) EditorApplication.Exit(phase == 1 ? 0 : 1);
            return;
        }
        try
        {
            Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + ".Deadline", 0), "Play mode timed out");
            if (!EditorApplication.isPlaying || ScreenTransition.IsBusy) return;
            var controller = UnityEngine.Object.FindFirstObjectByType<DialogueController>();
            Require(controller != null, "Missing dialogue controller");
            Check(controller, false);
            Check(controller, true);
            Debug.Log("DIALOGUE_SCENE_CUE_VALIDATION_PASS: manual and cast conversations; background inheritance, crop and reset; sound once per line; music change, keep, stop, restart, replay and cleanup; pagination, reveal and hide; volume; authored Canvas unchanged.");
            SessionState.SetInt(Key + ".Phase", 1);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            SessionState.SetInt(Key + ".Phase", 2);
        }
        EditorApplication.isPlaying = false;
    }

    private static void Check(DialogueController controller, bool cast)
    {
        var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
        var actor = ScriptableObject.CreateInstance<DialogueCharacter>();
        var room = new Texture2D(16, 9);
        var stairs = new Texture2D(9, 16);
        var fallback = new Texture2D(4, 3);
        var clip = AudioClip.Create("Fall test", 44100, 1, 44100, false);
        var musicA = AudioClip.Create("Music A", 44100, 1, 44100, false);
        var musicB = AudioClip.Create("Music B", 44100, 1, 44100, false);
        var musicC = AudioClip.Create("Music C", 44100, 1, 44100, false);
        try
        {
            Set(sequence, "background", room);
            Set(sequence, "music", musicA);
            if (cast) Set(sequence, "characters", new[] { actor });
            Set(sequence, "lines", new[] {
                new DialogueLine { character = cast ? actor : null, speaker = "Room", text = "Start." },
                new DialogueLine { character = cast ? actor : null, speaker = "Stairs", text = string.Join(" ", Enumerable.Repeat("Texto largo para probar varias páginas.", 60)), background = stairs, soundEffect = clip, soundEffectVolume = 0.4f, musicAction = DialogueMusicAction.Play, music = musicB },
                new DialogueLine { character = cast ? actor : null, speaker = "Same stairs", text = "Keep the previous background." },
                new DialogueLine { character = cast ? actor : null, speaker = "Back", text = "Return.", background = room, musicAction = DialogueMusicAction.Stop },
                new DialogueLine { character = cast ? actor : null, text = "Silence continues." },
                new DialogueLine { character = cast ? actor : null, text = "Third track.", musicAction = DialogueMusicAction.Play, music = musicC },
                new DialogueLine { character = cast ? actor : null, text = "Original track again.", musicAction = DialogueMusicAction.Play, music = musicA }
            });
            var screen = Field<RectTransform>(controller, "screen");
            int count = screen.GetComponentsInChildren<Transform>(true).Length;
            bool completed = false;
            controller.Play(sequence, fallback, () => completed = true);
            var backdrop = Field<RawImage>(controller, "backdrop");
            Require(backdrop.texture == room, "Sequence background overrides fallback");
            var pages = Field<IList>(controller, "pages");
            Require(pages.Count > 7, "Test must span multiple pages");
            Require(pages.Cast<object>().Count(p => Field<AudioClip>(p, "soundEffect") != null) == 1, "Sound must only appear on first page of line");
            Require(pages.Cast<object>().Count(p => Field<DialogueMusicAction>(p, "musicAction") != DialogueMusicAction.Keep) == 4,
                "Music changes must only appear on first page of each authored line");
            Texture expected = room;
            AudioClip expectedMusic = musicA;
            for (int i = 0; i < pages.Count; i++)
            {
                var background = Field<Texture2D>(pages[i], "background");
                if (background != null) expected = background;
                var musicAction = Field<DialogueMusicAction>(pages[i], "musicAction");
                if (musicAction == DialogueMusicAction.Play) expectedMusic = Field<AudioClip>(pages[i], "music");
                else if (musicAction == DialogueMusicAction.Stop) expectedMusic = null;
                var musicAudio = StaticField<GameAudio>(typeof(GameAudio), "instance");
                Require(Field<AudioClip>(musicAudio, "targetMusic") == expectedMusic, "Music changes, stop and inheritance must follow page order");
                var transition = Field<Coroutine>(musicAudio, "musicTransition");
                Require(backdrop.texture == expected, "Background changes and persists on the correct page");
                Require(backdrop.uvRect.width > 0 && backdrop.uvRect.width <= 1 && backdrop.uvRect.height > 0 && backdrop.uvRect.height <= 1,
                    "Background crop must preserve cover bounds");
                if (Field<AudioClip>(pages[i], "soundEffect") != null)
                {
                    var audio = StaticField<GameAudio>(typeof(GameAudio), "instance");
                    Require(Field<UnityEngine.Object>(audio, "dialogueOwner") == controller, "Effect plays on entry");
                    Require(Mathf.Approximately(Field<float>(pages[i], "soundEffectVolume"), 0.4f), "Authored effect volume survives pagination");
                    Require(Mathf.Approximately(Field<AudioSource>(audio, "dialogueSource").volume, GamePreferences.EffectsVolume), "Effects preference applied");
                }
                GameAudio.StopDialogueEffects(controller);
                controller.HideDialogue(); controller.RestoreDialogue();
                if (controller.IsTyping) controller.Advance();
                Require(Field<AudioClip>(musicAudio, "targetMusic") == expectedMusic && Field<Coroutine>(musicAudio, "musicTransition") == transition,
                    "Reveal and hide must not restart music or its fade");
                var activeAudio = StaticField<GameAudio>(typeof(GameAudio), "instance");
                Require(Field<UnityEngine.Object>(activeAudio, "dialogueOwner") == null, "Reveal/hide must not replay effect");
                controller.Advance();
            }
            Require(completed && !controller.IsPlaying, "Conversation completes normally");
            var finishedAudio = StaticField<GameAudio>(typeof(GameAudio), "instance");
            Require(Field<AudioClip>(finishedAudio, "targetMusic") == null, "Completion stops the final track");
            Require(count == screen.GetComponentsInChildren<Transform>(true).Length, "No runtime UI creation");
            controller.Play(sequence, fallback);
            Require(backdrop.texture == room, "Replay resets initial background");
            Require(Field<AudioClip>(finishedAudio, "targetMusic") == musicA, "Replay restores initial music");
            GameAudio.PlayDialogueEffect(clip, 1, controller);
            var instance = StaticField<GameAudio>(typeof(GameAudio), "instance");
            GameAudio.StopDialogueEffects(actor);
            Require(Field<UnityEngine.Object>(instance, "dialogueOwner") == controller, "Old owner cannot stop active sound");
            controller.Cancel();
            Require(Field<UnityEngine.Object>(instance, "dialogueOwner") == null, "Cancel clears conversation sound");
            Require(Field<AudioClip>(instance, "targetMusic") == null, "Cancel stops music");
            var invalidLine = new DialogueLine { text = "Missing music", musicAction = DialogueMusicAction.Play };
            Set(sequence, "lines", new[] { invalidLine });
            Require(!sequence.ValidateMusic(out _), "Changing music requires a clip");
            invalidLine.musicAction = DialogueMusicAction.Keep;
            Require(sequence.ValidateMusic(out _), "Default keep supports existing dialogue assets");
            Set(sequence, "music", null);
            Set(sequence, "background", null);
            controller.Play(sequence, fallback);
            Require(backdrop.texture == fallback, "Legacy fallback remains supported");
            controller.Cancel();
            controller.Play(sequence);
            Require(!backdrop.gameObject.activeSelf, "No background leaks to next conversation");
            controller.Cancel();
        }
        finally
        {
            controller.Cancel();
            foreach (var item in new UnityEngine.Object[] { sequence, actor, room, stairs, fallback, clip, musicA, musicB, musicC }) UnityEngine.Object.Destroy(item);
        }
    }

    private static T Field<T>(object source, string field) => (T)source.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(source);
    private static T StaticField<T>(Type type, string field) => (T)type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    private static void Set(object source, string field, object value) => source.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, value);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
