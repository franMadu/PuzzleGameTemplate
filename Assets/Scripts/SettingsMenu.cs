using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PuzzleUI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SettingsMenu : MonoBehaviour
{
    private Action close;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private int resolutionIndex;
    private bool fullscreen;
    private Button fullscreenButton;
    private Text resolutionLabel;

    public void Build(Action onClose)
    {
        close = onClose;
        var content = (RectTransform)transform;
        Label(content, "CONFIGURACIÓN", 0, 410, 680, 65, 34, Ink);
        Label(content, "SONIDO", 0, 342, 680, 44, 23, Ink);
        Volume(content, "Volumen general", "Master Volume", 285, GamePreferences.MasterVolume, GamePreferences.SetMasterVolume);
        Volume(content, "Música", "Music Volume", 230, GamePreferences.MusicVolume, GamePreferences.SetMusicVolume);
        Volume(content, "Efectos de sonido", "Effects Volume", 175, GamePreferences.EffectsVolume, GamePreferences.SetEffectsVolume);
        var mute = Button(content, "", 0, 115, 540, 48, () => { });
        var muteText = Label((RectTransform)mute.transform, MuteLabel(), 0, 0, 520, 44, 21, Ink);
        mute.name = "Mute";
        mute.onClick.AddListener(() => { GamePreferences.SetMuted(!GamePreferences.Muted); muteText.text = MuteLabel(); });

        Label(content, "DIÁLOGOS", 0, 58, 680, 44, 23, Ink);
        var speed = Button(content, "", 0, 0, 540, 48, () => { });
        speed.name = "Text Speed";
        var speedText = Label((RectTransform)speed.transform, SpeedLabel(), 0, 0, 520, 44, 21, Ink);
        speed.onClick.AddListener(() => { GamePreferences.SetTextSpeed((GamePreferences.TextSpeed + 1) % 4); speedText.text = SpeedLabel(); });
        Volume(content, "Opacidad del cuadro", "Dialogue Opacity", -60, GamePreferences.DialogueOpacity(), GamePreferences.SetDialogueOpacity, 0.45f);

        Label(content, "PANTALLA", 0, -125, 680, 44, 23, Ink);
        fullscreen = UnityEngine.Screen.fullScreen;
        fullscreenButton = Button(content, FullscreenLabel(), 0, -180, 540, 48, () =>
        {
            fullscreen = !fullscreen;
            var selected = resolutions[resolutionIndex];
            GamePreferences.ApplyDisplay(selected.x, selected.y, fullscreen);
            fullscreenButton.GetComponentInChildren<Text>().text = FullscreenLabel();
        });
        fullscreenButton.name = "Fullscreen";
        BuildResolutions();
        resolutionLabel = Label(content, ResolutionLabel(), 0, -239, 420, 46, 22, Ink);
        var previous = Button(content, "‹", -260, -239, 54, 48, () => ChangeResolution(-1));
        var next = Button(content, "›", 260, -239, 54, 48, () => ChangeResolution(1));
        previous.name = "Previous Resolution";
        next.name = "Next Resolution";
        fullscreenButton.interactable = GamePreferences.DisplaySupported;
        previous.interactable = next.interactable = GamePreferences.DisplaySupported && resolutions.Count > 1;
        if (!GamePreferences.DisplaySupported) resolutionLabel.text = "Automática";

#if UNITY_EDITOR
        var godMode = Button(content, "", 0, -296, 540, 42, () => { });
        godMode.name = "God Mode";
        var godText = Label((RectTransform)godMode.transform, GodModeLabel(), 0, 0, 520, 40, 21, Ink);
        godMode.onClick.AddListener(() => { GamePreferences.SetGodMode(!GamePreferences.GodMode); godText.text = GodModeLabel(); });
#endif
        Button(content, "Restablecer ajustes", 0, -355, 420, 48, () =>
        {
            GamePreferences.RestoreDefaults();
            foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Build(close);
        });
        Button(content, "Volver", 0, -420, 300, 60, Close);
    }

    private static string MuteLabel() => "Silenciar: " + (GamePreferences.Muted ? "Sí" : "No");
#if UNITY_EDITOR
    private static string GodModeLabel() => "Modo dios (editor): " + (GamePreferences.GodMode ? "Activado" : "Desactivado");
#endif
    private static string SpeedLabel() => "Velocidad del texto: " + new[] { "Lenta", "Normal", "Rápida", "Instantánea" }[GamePreferences.TextSpeed];
    private string FullscreenLabel() => "Modo: " + (fullscreen ? "Pantalla completa" : "Ventana");
    private string ResolutionLabel() => $"{resolutions[resolutionIndex].x} × {resolutions[resolutionIndex].y}";

    private void BuildResolutions()
    {
        resolutions.Clear();
        foreach (var resolution in UnityEngine.Screen.resolutions)
        {
            var size = new Vector2Int(resolution.width, resolution.height);
            if (size.x >= 640 && size.y >= 480 && !resolutions.Contains(size)) resolutions.Add(size);
        }
        var current = new Vector2Int(UnityEngine.Screen.width, UnityEngine.Screen.height);
        if (!resolutions.Contains(current)) resolutions.Add(current);
        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        resolutionIndex = resolutions.IndexOf(current);
    }

    private void ChangeResolution(int direction)
    {
        resolutionIndex = (resolutionIndex + direction + resolutions.Count) % resolutions.Count;
        var size = resolutions[resolutionIndex];
        GamePreferences.ApplyDisplay(size.x, size.y, fullscreen);
        resolutionLabel.text = ResolutionLabel();
    }

    private static void Volume(RectTransform parent, string title, string name, float y, float value, Action<float> changed, float minimum = 0)
    {
        var label = Label(parent, title, -185, y, 270, 45, 21, Ink);
        label.alignment = TextAnchor.MiddleLeft;
        var rect = Rect(name, parent, 105, y, 270, 44);
        rect.gameObject.AddComponent<Image>().color = Color.clear;
        var slider = rect.gameObject.AddComponent<Slider>();
        var track = Rect("Track", rect, 0, 0, 270, 8);
        track.gameObject.AddComponent<Image>().color = Panel;
        var fill = Rect("Fill", track, 0, 0, 0, 0);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
        fill.gameObject.AddComponent<Image>().color = Accent;
        var handleArea = Rect("Handle Area", rect, 0, 0, 246, 44);
        var handle = Rect("Handle", handleArea, 0, 0, 24, 30);
        var graphic = handle.gameObject.AddComponent<Image>(); graphic.color = Ink;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = graphic;
        slider.minValue = minimum;
        slider.maxValue = 1;
        slider.SetValueWithoutNotify(value);
        var percent = Label(parent, $"{value:P0}", 290, y, 85, 44, 19, Muted);
        slider.onValueChanged.AddListener(amount => { changed(amount); percent.text = $"{amount:P0}"; });
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
