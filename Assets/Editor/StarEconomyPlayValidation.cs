using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StarEconomyPlayValidation
{
    private const string Key = "StarEconomyPlayValidation";
    private static PuzzleCatalog catalog, snapshot;
    private static string game;
    static StarEconomyPlayValidation() => EditorApplication.update += Tick;

    [MenuItem("Tools/One Hand Puzzle/Validate Star Economy Flow (Play Mode)")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        StarEconomyValidation.Run();
        EngineInteractionValidation.Run();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuzzleMenu>(true)).Single().enabled = false;
        SessionState.SetBool(Key, true);
        SessionState.SetInt(Key + ".Phase", 0);
        SessionState.SetFloat(Key + ".Deadline", (float)EditorApplication.timeSinceStartup + 120);
        EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        int phase = SessionState.GetInt(Key + ".Phase", 0);
        if (phase == 3 || phase == 4)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(Key, false);
            if (Application.isBatchMode) EditorApplication.Exit(phase == 3 ? 0 : 1);
            return;
        }
        try
        {
            Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + ".Deadline", 0), "Economy flow timed out");
            if (!EditorApplication.isPlaying || ScreenTransition.IsBusy) return;
            if (phase == 0)
            {
                var menu = FindMenu();
                catalog = Field<PuzzleCatalog>(menu, "catalog");
                snapshot = UnityEngine.Object.Instantiate(catalog);
                game = "EconomyFlow." + Guid.NewGuid().ToString("N");
                var image = catalog[0].image;
                Set(catalog, "progressId", game);
                Set(catalog, "story", null);
                Set(catalog, "chapters", Enumerable.Range(0, 3).Select(i => new PuzzleChapter {
                    id = "extra-" + i, title = "Flow puzzle " + i, image = image, normalImage = image, hardImage = image,
                    starCost = i == 0 ? 0 : i == 1 ? 99 : 2 }).ToArray());
                PuzzleProgress.Complete(0, 0, 3, 10, 10, game, false);
                PuzzleSession.TitleDismissed = true;
                Invoke(menu, "Start");
                var main = Field<RectTransform>(menu, "mainScreen").Find("Content");
                Require(MenuControls.Get<Text>(main, "Wallet/Amount").text == "3", "Main menu wallet shows earned stars");
                // Bindings invoke transition gating; skip only visual timing between panels in this synchronous phase.
                Invoke(menu, "ShowShop", 0, true);
                var shop = Field<RectTransform>(menu, "shopScreen").Find("Content");
                Require(!MenuControls.Get<Button>(shop, "Row 0/Buy").interactable, "Unaffordable purchase disabled");
                SessionState.SetInt(Key + ".Phase", 1);
            }
            else if (phase == 1)
            {
                var menu = FindMenu();
                var shop = Field<RectTransform>(menu, "shopScreen").Find("Content");
                MenuControls.Get<Button>(shop, "Row 1/Buy").onClick.Invoke();
                Require(StarEconomy.Balance(catalog) == 1 && StarEconomy.OwnsPuzzle(catalog, 2), "Buy click charges and grants ownership");
                Require(MenuControls.Get<Text>(shop, "Wallet/Amount").text == "1", "Shop wallet refreshes after purchase");
                Require(!StarEconomy.TryBuyPuzzle(catalog, 2) && StarEconomy.Balance(catalog) == 1, "Repeated purchase cannot debit twice");
                MenuControls.Get<Button>(shop, "Row 1/Buy").onClick.Invoke();
                SessionState.SetInt(Key + ".Phase", 10);
            }
        }
        catch (Exception error) { Fail(error); }
    }

    // UI transitions span frames; these later phases keep using the real button callbacks.
    [InitializeOnLoadMethod]
    private static void BindLaterPhases() => EditorApplication.update += Later;
    private static void Later()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || ScreenTransition.IsBusy) return;
        int phase = SessionState.GetInt(Key + ".Phase", 0);
        try
        {
            if (phase == 10)
            {
                var menu = FindMenu();
                var content = Field<RectTransform>(menu, "difficultyScreen").Find("Content");
                MenuControls.Get<Button>(content, "Difficulty 2").onClick.Invoke();
                MenuControls.Get<Button>(content, "Play").onClick.Invoke();
                SessionState.SetInt(Key + ".Phase", 11);
            }
            else if (phase == 11 && SceneManager.GetActiveScene().name == "PuzzleHard")
            {
                var controller = UnityEngine.Object.FindFirstObjectByType<PuzzleSceneController>();
                Require(Field<int>(controller, "chapter") == 2 && PuzzleProgress.Unlocked(3, game) == 1,
                    "Purchased puzzle beyond sequential progress launches in selected difficulty");
                var puzzle = Field<Puzzle>(controller, "puzzle");
                Set(puzzle, "moves", puzzle.MinimumMoves);
                Invoke(controller, "OnCompleted");
                Invoke(controller, "ShowVictory");
                Require(StarEconomy.Balance(catalog) == 4 && PuzzleProgress.Unlocked(3, game) == 1, "Completion awards stars without unlocking other paid puzzles");
                var victory = Field<RectTransform>(controller, "victoryScreen").Find("Content");
                Require(MenuControls.Get<Text>(victory, "Star Reward").text.Contains("+3"), "Victory displays earned delta");
                Invoke(controller, "OnCompleted"); Invoke(controller, "ShowVictory");
                Require(StarEconomy.Balance(catalog) == 4 && MenuControls.Get<Text>(victory, "Star Reward").text.Contains("+0"), "Duplicate completion does not award again");
                MenuControls.Get<Button>(victory, "Back").onClick.Invoke();
                SessionState.SetInt(Key + ".Phase", 12);
            }
            else if (phase == 12 && SceneManager.GetActiveScene().name == "MainMenu")
            {
                var menu = FindMenu();
                // The initial scene fixture disables Start until its isolated save is assigned.
                if (!menu.enabled) { menu.enabled = true; return; }
                if (Field<RectTransform>(menu, "screen") == null) return;
                Require(MenuControls.Get<Text>(Field<RectTransform>(menu, "mainScreen").Find("Content"), "Wallet/Amount").text == "4",
                    "Menu reload refreshes persisted balance");
                Cleanup();
                Debug.Log("STAR_ECONOMY_PLAY_VALIDATION_PASS: real shop purchase buttons, disabled unaffordable item, wallet refresh, hard difficulty launch beyond legacy unlock, completion event rewards, repeated completion, return to main menu and persisted balance.");
                SessionState.SetInt(Key + ".Phase", 3);
                EditorApplication.isPlaying = false;
            }
        }
        catch (Exception error) { Fail(error); }
    }
    private static void Fail(Exception error)
    {
        Debug.LogException(error); Cleanup();
        SessionState.SetInt(Key + ".Phase", 4); EditorApplication.isPlaying = false;
    }
    private static void Cleanup()
    {
        if (catalog != null && snapshot != null) EditorUtility.CopySerialized(snapshot, catalog);
        if (game == null) return;
        PlayerPrefs.DeleteKey(game + ".StarEconomy.v1"); PlayerPrefs.DeleteKey(game + ".Unlocked");
        for (int p = 0; p < 3; p++)
        {
            PlayerPrefs.DeleteKey(game + ".Completed." + p);
            for (int d = 0; d < 3; d++) PlayerPrefs.DeleteKey(game + ".BestStars." + p + "." + d);
        }
        PlayerPrefs.Save();
    }
    private static PuzzleMenu FindMenu() => UnityEngine.Object.FindFirstObjectByType<PuzzleMenu>(FindObjectsInactive.Include);
    private static T Field<T>(object source, string name) => (T)source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
    private static void Set(object source, string name, object value) => source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, value);
    private static void Invoke(object source, string name, params object[] args) => source.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source, args);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
