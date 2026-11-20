using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class StarEconomyValidation
{
    [MenuItem("Tools/One Hand Puzzle/Validate Star Economy")]
    public static void Run()
    {
        string game = "EconomyValidation." + Guid.NewGuid().ToString("N");
        var catalog = ScriptableObject.CreateInstance<PuzzleCatalog>();
        var image = new Texture2D(4, 4);
        Set(catalog, "progressId", game);
        var puzzles = Enumerable.Range(0, 5).Select(i => new PuzzleChapter {
            id = "puzzle-" + i, title = "Puzzle " + i, image = image, normalImage = image, hardImage = image,
            starCost = i == 1 ? 2 : i == 2 ? 5 : 0
        }).ToArray();
        Set(catalog, "chapters", puzzles);
        var wallpapers = Enumerable.Range(0, 8).Select(i => new GalleryWallpaper { id = "wall-" + i, image = image, starCost = 2 }).ToArray();
        // Initial and shop wallpapers must be distinct textures for ownership checks.
        var initial = new Texture2D(2, 2);
        Set(catalog, "menuBackground", initial);
        wallpapers[0].image = initial;
        Set(catalog, "wallpapers", wallpapers);
        try
        {
            int Complete(int puzzle, int difficulty, int moves) => PuzzleProgress.Complete(puzzle, difficulty, 5, moves, 10, game, puzzles[puzzle].starCost == 0);
            Require(StarEconomy.Balance(catalog) == 0, "New save starts empty");
            Require(Complete(0, 0, 40) == 1 && StarEconomy.Balance(catalog) == 1, "First one-star result earns one");
            Require(Complete(0, 0, 40) == 0 && StarEconomy.Balance(catalog) == 1, "Same score never pays twice");
            Require(!StarEconomy.TryBuyPuzzle(catalog, 1) && StarEconomy.Balance(catalog) == 1, "Insufficient funds change nothing");
            Require(Complete(0, 0, 15) == 1 && StarEconomy.Balance(catalog) == 2, "One to two stars earns only the difference");
            Require(StarEconomy.TryBuyPuzzle(catalog, 1) && StarEconomy.Balance(catalog) == 0 && StarEconomy.CanPlayPuzzle(catalog, 1), "Purchase spends stars and grants access");
            Require(!StarEconomy.TryBuyPuzzle(catalog, 1) && !StarEconomy.TryBuyWallpaper(catalog, 0), "Owned purchases never charge again");
            Require(Complete(0, 0, 40) == 0 && StarEconomy.Balance(catalog) == 0, "Spending cannot reset earned records");
            int unlocked = PuzzleProgress.Unlocked(5, game);
            Require(Complete(1, 0, 40) == 1 && PuzzleProgress.Unlocked(5, game) == unlocked, "Purchased challenge rewards do not unlock the next puzzle");
            Require(Complete(0, 0, 10) == 1 && StarEconomy.Balance(catalog) == 2, "Two to three stars earns one");
            Require(StarEconomy.TryBuyWallpaper(catalog, 1) && StarEconomy.Balance(catalog) == 0, "Wallpaper is purchased");
            Require(WallpaperProgress.IsUnlocked(catalog, 1), "Purchased wallpaper joins gallery");
            GamePreferences.SetMenuBackground(catalog, 1);
            Require(GamePreferences.MenuBackgroundIndex(catalog) == 1, "Purchased wallpaper can be selected");
            Require(!StarEconomy.TryBuyWallpaper(catalog, 1) && StarEconomy.Balance(catalog) == 0, "Wallpaper cannot charge twice");
            Require(Complete(1, 0, 10) == 2 && StarEconomy.Balance(catalog) == 2, "One to three stars earns two");
            Require(Complete(0, 1, 10) == 3 && StarEconomy.Balance(catalog) == 5, "Each difficulty has an independent record");
            Require(StarEconomy.TryBuyPuzzle(catalog, 2) && StarEconomy.Balance(catalog) == 0 && StarEconomy.CanPlayPuzzle(catalog, 2), "Bought puzzle beyond sequential unlock can launch");
            Require(Complete(2, 0, 40) == 1 && Complete(2, 0, 40) == 0, "New challenge follows same anti-repeat rule");
            PlayerPrefs.SetInt(game + ".BestGrade.3.2", 1);
            Require(StarEconomy.Balance(catalog) == 4 && StarEconomy.Balance(catalog) == 4, "Legacy grade migrates once");
            Require(Complete(3, 2, 40) == 0 && StarEconomy.Balance(catalog) == 4, "Legacy maximum cannot pay twice");
            string persisted = PlayerPrefs.GetString(game + ".StarEconomy.v1");
            PlayerPrefs.SetString(game + ".StarEconomy.v1", persisted);
            Require(StarEconomy.Balance(catalog) == 4 && StarEconomy.OwnsPuzzle(catalog, 2) && StarEconomy.OwnsWallpaper(catalog, 1), "Reload preserves balance, purchases and spent stars");
            PlayerPrefs.SetInt(game + ".Wallpaper.Unlocked.wall-2", 1);
            Require(!StarEconomy.TryBuyWallpaper(catalog, 2) && StarEconomy.Balance(catalog) == 4, "Story reward is never charged");
            Require(!StarEconomy.TryBuyPuzzle(catalog, -1) && !StarEconomy.TryBuyWallpaper(catalog, 99), "Invalid items cannot charge");
            puzzles[2].id = puzzles[1].id;
            Require(!StarEconomy.TryBuyPuzzle(catalog, 2), "Duplicate product IDs rejected");
            puzzles[2].id = "puzzle-2";
            ValidateCanvas(catalog);
            Debug.Log("STAR_ECONOMY_VALIDATION_PASS: score deltas 1/0/1/1 and 1-to-3; spending without farming; independent difficulties; legacy migration; persisted purchases; insufficient funds; duplicate IDs; story rewards; shop pagination, labels, wallet and unchanged Canvas object count.");
        }
        finally
        {
            PlayerPrefs.DeleteKey(game + ".StarEconomy.v1");
            PlayerPrefs.DeleteKey(game + ".Unlocked");
            PlayerPrefs.DeleteKey("Game.Settings.MenuWallpaper." + game);
            for (int p = 0; p < 5; p++)
            {
                PlayerPrefs.DeleteKey(game + ".Completed." + p);
                for (int d = 0; d < 3; d++)
                { PlayerPrefs.DeleteKey(game + ".BestStars." + p + "." + d); PlayerPrefs.DeleteKey(game + ".BestGrade." + p + "." + d); }
            }
            PlayerPrefs.DeleteKey(game + ".Wallpaper.Unlocked.wall-2");
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(initial);
        }
    }
    private static void ValidateCanvas(PuzzleCatalog catalog)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
        try
        {
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuzzleMenu>(true)).Single();
            var shopScreen = Field<RectTransform>(menu, "shopScreen");
            var shop = shopScreen.Find("Content").GetComponent<StarShop>();
            int before = shopScreen.GetComponentsInChildren<Transform>(true).Length;
            int page = -1; bool category = false;
            shop.Build(catalog, 0, false, (p, c) => { page = p; category = c; }, _ => { }, () => { });
            Require(shop.transform.Find("Pages").gameObject.activeSelf, "Shop supports more than six items");
            Require(MenuControls.Get<Text>(shop.transform, "Wallet/Amount").text == "4", "Wallet displays spendable balance");
            Require(MenuControls.Get<Text>(shop.transform, "Row 1/Buy/Label").text == "Usar fondo", "Owned item offers use instead of purchase");
            shop.Build(catalog, int.MaxValue, false, (_, __) => { }, _ => { }, () => { });
            Require(!shop.transform.Find("Row 2").gameObject.activeSelf, "Last shop page reuses two cards");
            shop.Build(catalog, 0, true, (p, c) => { page = p; category = c; }, _ => { }, () => { });
            Require(MenuControls.Get<Text>(shop.transform, "Row 0/Buy/Label").text == "Jugar", "Owned challenge can be opened");
            Require(before == shopScreen.GetComponentsInChildren<Transform>(true).Length, "Shop creates no runtime UI");
            var main = Field<RectTransform>(menu, "mainScreen").Find("Content");
            var icon = MenuControls.Get<PuzzleStarsGraphic>(main, "Wallet/Icon");
            Require(icon.Slots == 1 && icon.Stars == 1 && !icon.raycastTarget, "Wallet uses one authored star icon");
            Require(MenuControls.Get<Button>(main, "Navigation/Shop").GetComponentInChildren<Text>().text == "Tienda", "Shop navigation button has visible text");
            foreach(string field in new[]{"mainScreen","galleryScreen","chaptersScreen","subchaptersScreen","selectionScreen","difficultyScreen","settingsScreen","shopScreen","challengesScreen"})
            {
                var content=Field<RectTransform>(menu,field).Find("Content");
                Require(content.Find("Wallet/Amount")!=null,"Every menu has the same wallet location");
                foreach(string item in new[]{"Home","Chapters","Gallery","Challenges","Shop"})
                {
                    var symbol=content.Find("Navigation/"+item+"/Icon").GetComponent<MenuIconGraphic>();
                    Require(symbol!=null && symbol.canvasRenderer!=null,"Five visible authored navigation icons");
                }
            }
            var challenge=Field<RectTransform>(menu,"challengesScreen").Find("Content").GetComponent<PuzzleChallenges>();
            int count=challenge.GetComponentsInChildren<Transform>(true).Length;
            challenge.Build(catalog,0,_=>{},_=>{},()=>{},()=>{});
            challenge.Build(catalog,int.MaxValue,_=>{},_=>{},()=>{},()=>{});
            Require(count==challenge.GetComponentsInChildren<Transform>(true).Length,"Challenges reuse authored cards");
            var gallery=Field<RectTransform>(menu,"galleryScreen").Find("Content");
            for(int i=0;i<6;i++)Require(gallery.Find("Image "+i).GetComponentsInChildren<Text>(true).Length==0,"Gallery cards remain image-only");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    private static T Field<T>(object source, string field) => (T)source.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
    private static void Set(object source, string field, object value) => source.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, value);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
