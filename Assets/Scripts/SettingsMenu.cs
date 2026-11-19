using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SettingsMenu : MonoBehaviour
{
    private Action close;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private int resolutionIndex;
    private bool fullscreen;
    private Text resolutionLabel;

    public void Build(Action onClose)
    {
        close = onClose;
        Volume("Master Volume", GamePreferences.MasterVolume, GamePreferences.SetMasterVolume);
        Volume("Music Volume", GamePreferences.MusicVolume, GamePreferences.SetMusicVolume);
        Volume("Effects Volume", GamePreferences.EffectsVolume, GamePreferences.SetEffectsVolume);
        Volume("Dialogue Opacity", GamePreferences.DialogueOpacity(), GamePreferences.SetDialogueOpacity);
        MenuControls.Bind(transform, "Mute", () =>
        {
            GamePreferences.SetMuted(!GamePreferences.Muted);
            RefreshLabels();
        });
        MenuControls.Bind(transform, "Text Speed", () =>
        {
            GamePreferences.SetTextSpeed((GamePreferences.TextSpeed + 1) % 4);
            RefreshLabels();
        });
        fullscreen = Screen.fullScreen;
        BuildResolutions();
        resolutionLabel = MenuControls.Get<Text>(transform, "Resolution");
        RefreshResolution();
        MenuControls.Bind(transform, "Fullscreen", () =>
        {
            fullscreen = !fullscreen;
            var size = resolutions[resolutionIndex];
            GamePreferences.ApplyDisplay(size.x, size.y, fullscreen);
            RefreshLabels();
        }).interactable = GamePreferences.DisplaySupported;
        MenuControls.Bind(transform, "Previous Resolution", () => ChangeResolution(-1)).interactable =
            GamePreferences.DisplaySupported && resolutions.Count > 1;
        MenuControls.Bind(transform, "Next Resolution", () => ChangeResolution(1)).interactable =
            GamePreferences.DisplaySupported && resolutions.Count > 1;
        transform.Find("God Mode").gameObject.SetActive(Application.isEditor);
#if UNITY_EDITOR
        MenuControls.Bind(transform, "God Mode", () =>
        {
            GamePreferences.SetGodMode(!GamePreferences.GodMode);
            RefreshLabels();
        });
#endif
        MenuControls.Bind(transform, "Restore Defaults", () =>
        {
            GamePreferences.RestoreDefaults();
            Build(close);
        });
        MenuControls.Bind(transform, "Back", Close);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        CanvasText.Set(MenuControls.Get<Text>(transform, "Mute/Label"), GamePreferences.Muted ? 1 : 0);
        CanvasText.Set(MenuControls.Get<Text>(transform, "Text Speed/Label"), GamePreferences.TextSpeed);
        CanvasText.Set(MenuControls.Get<Text>(transform, "Fullscreen/Label"), fullscreen ? 1 : 0);
#if UNITY_EDITOR
        CanvasText.Set(MenuControls.Get<Text>(transform, "God Mode/Label"), GamePreferences.GodMode ? 1 : 0);
#endif
    }

    private void RefreshResolution()
    {
        var size = resolutions[resolutionIndex];
        CanvasText.Set(resolutionLabel, GamePreferences.DisplaySupported ? 0 : 1, size.x, size.y);
    }

    private void BuildResolutions()
    {
        resolutions.Clear();
        foreach (var resolution in Screen.resolutions)
        {
            var size = new Vector2Int(resolution.width, resolution.height);
            if (size.x >= 640 && size.y >= 480 && !resolutions.Contains(size)) resolutions.Add(size);
        }
        var current = new Vector2Int(Screen.width, Screen.height);
        if (!resolutions.Contains(current)) resolutions.Add(current);
        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        resolutionIndex = resolutions.IndexOf(current);
    }

    private void ChangeResolution(int direction)
    {
        resolutionIndex = (resolutionIndex + direction + resolutions.Count) % resolutions.Count;
        var size = resolutions[resolutionIndex];
        GamePreferences.ApplyDisplay(size.x, size.y, fullscreen);
        RefreshResolution();
    }

    private void Volume(string path, float value, Action<float> changed)
    {
        var slider = MenuControls.Get<Slider>(transform, path);
        var percent = MenuControls.Get<Text>(transform, path + "/Percent");
        slider.onValueChanged.RemoveAllListeners();
        slider.SetValueWithoutNotify(value);
        CanvasText.Set(percent, 0, value);
        slider.onValueChanged.AddListener(amount => { changed(amount); CanvasText.Set(percent, 0, amount); });
    }

    private void Close() { GamePreferences.Save(); close?.Invoke(); }
    private void OnDisable() => GamePreferences.Save();
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
    }
}
