using UnityEngine;

// PlayerPrefs already separates installed games by company/product; these keys never touch story saves.
public static class GamePreferences
{
    private const string Prefix = "Game.Settings.";
    public static int MenuBackgroundIndex(PuzzleCatalog catalog)
    {
        // A separate key intentionally ignores old puzzle-image background selections.
        string selected = PlayerPrefs.GetString(Prefix + "MenuWallpaper." + catalog.ProgressId, "");
        if (string.IsNullOrEmpty(selected)) return -1;
        for (int i = 0; i < catalog.WallpaperCount; i++)
        {
            var wallpaper = catalog.WallpaperAt(i);
            if (wallpaper?.image != null && wallpaper.Id == selected) return i;
        }
        return -1;
    }

    public static void SetMenuBackground(PuzzleCatalog catalog, int index)
    {
        var wallpaper = catalog.WallpaperAt(index);
        if (wallpaper?.image == null) return;
        PlayerPrefs.SetString(Prefix + "MenuWallpaper." + catalog.ProgressId, wallpaper.Id);
        Save();
    }

    public static Texture2D MenuBackground(PuzzleCatalog catalog)
    {
        int index = MenuBackgroundIndex(catalog);
        return index >= 0 ? catalog.WallpaperAt(index).image : catalog.MenuBackground;
    }
    public static bool GodMode
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool("Puzzle.GodMode." + Application.dataPath, false);
#else
            return false;
#endif
        }
    }

    public static void SetGodMode(bool enabled)
    {
#if UNITY_EDITOR
        UnityEditor.EditorPrefs.SetBool("Puzzle.GodMode." + Application.dataPath, enabled);
#endif
    }
    public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Master", 1));
    public static float EffectsVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Effects", 1));
    public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Music", 1));
    public static bool Muted => PlayerPrefs.GetInt(Prefix + "Muted", 0) != 0;
    public static int TextSpeed => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "TextSpeed", 1), 0, 3);
    public static float DialogueOpacity(float fallback = 0.78f) => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Opacity", fallback), 0.45f, 1);
    public static float CharactersPerSecond(float normal)
    {
        int speed = TextSpeed;
        return normal * (speed == 3 ? 0 : speed == 0 ? 0.5f : speed == 2 ? 2 : 1);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        ApplyAudio();
        if (PlayerPrefs.HasKey(Prefix + "Fullscreen"))
            ApplyDisplay(PlayerPrefs.GetInt(Prefix + "Width", Screen.width), PlayerPrefs.GetInt(Prefix + "Height", Screen.height),
                PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) != 0, false);
    }

    public static void SetMasterVolume(float value) { PlayerPrefs.SetFloat(Prefix + "Master", Mathf.Clamp01(value)); ApplyAudio(); }
    public static void SetEffectsVolume(float value) { PlayerPrefs.SetFloat(Prefix + "Effects", Mathf.Clamp01(value)); }
    public static void SetMusicVolume(float value) { PlayerPrefs.SetFloat(Prefix + "Music", Mathf.Clamp01(value)); }
    public static void SetMuted(bool value) { PlayerPrefs.SetInt(Prefix + "Muted", value ? 1 : 0); ApplyAudio(); Save(); }
    public static void SetTextSpeed(int value) { PlayerPrefs.SetInt(Prefix + "TextSpeed", Mathf.Clamp(value, 0, 3)); Save(); }
    public static void SetDialogueOpacity(float value) { PlayerPrefs.SetFloat(Prefix + "Opacity", Mathf.Clamp(value, 0.45f, 1)); }
    public static void ApplyAudio() => AudioListener.volume = Muted ? 0 : MasterVolume;
    public static void Save() => PlayerPrefs.Save();

    public static void ApplyDisplay(int width, int height, bool fullscreen, bool save = true)
    {
        if (!DisplaySupported) return;
        width = Mathf.Max(640, width);
        height = Mathf.Max(480, height);
        Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        if (!save) return;
        PlayerPrefs.SetInt(Prefix + "Width", width);
        PlayerPrefs.SetInt(Prefix + "Height", height);
        PlayerPrefs.SetInt(Prefix + "Fullscreen", fullscreen ? 1 : 0);
        Save();
    }

    public static bool DisplaySupported => !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer;
    public static void RestoreDefaults()
    {
        SetGodMode(false);
        foreach (string field in new[] { "Master", "Effects", "Music", "Muted", "TextSpeed", "Opacity", "Width", "Height", "Fullscreen" })
            PlayerPrefs.DeleteKey(Prefix + field);
        ApplyAudio();
        if (DisplaySupported && !Application.isEditor)
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        Save();
    }
}
