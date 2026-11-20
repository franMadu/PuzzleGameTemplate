using System;
using System.Collections.Generic;
using UnityEngine;

// Wallet and purchases share one serialized account. Scores remain independent of spending.
public static class StarEconomy
{
    [Serializable] private class Record { public int puzzle; public int difficulty; public int stars; }
    [Serializable] private class Account
    {
        public int spent;
        public List<Record> records = new List<Record>();
        public List<string> purchases = new List<string>();
    }

    private static string Key(string game) => game + ".StarEconomy.v1";
    private static Account Read(string game)
    {
        var account = JsonUtility.FromJson<Account>(PlayerPrefs.GetString(Key(game), "{}")) ?? new Account();
        account.records = account.records ?? new List<Record>();
        account.purchases = account.purchases ?? new List<string>();
        return account;
    }
    private static void Save(string game, Account account)
    {
        PlayerPrefs.SetString(Key(game), JsonUtility.ToJson(account));
        PlayerPrefs.Save();
    }
    private static int Balance(Account account)
    {
        int earned = 0;
        foreach (var record in account.records) earned += record.stars;
        return Mathf.Max(0, earned - account.spent);
    }
    private static int Credit(Account account, int puzzle, int difficulty, int stars)
    {
        var record = account.records.Find(r => r.puzzle == puzzle && r.difficulty == difficulty);
        if (record == null)
        {
            record = new Record { puzzle = puzzle, difficulty = difficulty };
            account.records.Add(record);
        }
        int added = Mathf.Max(0, Mathf.Clamp(stars, 0, 3) - record.stars);
        record.stars += added;
        return added;
    }
    private static bool Synchronize(Account account, string game, int count)
    {
        bool changed = false;
        for (int puzzle = 0; puzzle < count; puzzle++)
            for (int difficulty = 0; difficulty < 3; difficulty++)
            {
                int stars = PuzzleProgress.BestStars(puzzle, difficulty, game);
                if (stars > 0) changed |= Credit(account, puzzle, difficulty, stars) > 0;
            }
        return changed;
    }
    public static int Balance(PuzzleCatalog catalog)
    {
        var account = Read(catalog.ProgressId);
        if (Synchronize(account, catalog.ProgressId, catalog.Count)) Save(catalog.ProgressId, account);
        return Balance(account);
    }
    // Called before saving the new best score, so legacy records are credited exactly once.
    public static int RecordResult(string game, int puzzle, int difficulty, int stars, int count)
    {
        var account = Read(game);
        Synchronize(account, game, count);
        int added = Credit(account, puzzle, difficulty, stars);
        Save(game, account);
        return added;
    }
    public static bool OwnsWallpaper(PuzzleCatalog catalog, int index)
    {
        var wallpaper = catalog.WallpaperAt(index);
        return wallpaper != null && !string.IsNullOrWhiteSpace(wallpaper.Id)
            && Read(catalog.ProgressId).purchases.Contains("wallpaper:" + wallpaper.Id);
    }
    public static bool OwnsPuzzle(PuzzleCatalog catalog, int index)
        => index >= 0 && index < catalog.Count && !string.IsNullOrWhiteSpace(catalog[index].id)
            && Read(catalog.ProgressId).purchases.Contains("puzzle:" + catalog[index].id);

    public static bool CanPlayPuzzle(PuzzleCatalog catalog, int index)
    {
        if (index < 0 || index >= catalog.Count) return false;
        if (OwnsPuzzle(catalog, index)) return true;
        if (catalog[index].starCost > 0) return false;
        return StoryProgress.IsPuzzleUnlocked(catalog, index)
            || (catalog.Story == null && index < PuzzleProgress.Unlocked(catalog.Count, catalog.ProgressId));
    }
    public static bool TryBuyWallpaper(PuzzleCatalog catalog, int index)
    {
        WallpaperProgress.Synchronize(catalog);
        var item = catalog.WallpaperAt(index);
        if (item?.image == null || item.starCost <= 0 || string.IsNullOrWhiteSpace(item.Id)
            || WallpaperProgress.IsUnlocked(catalog, index)) return false;
        for (int i = 0; i < catalog.WallpaperCount; i++)
            if (i != index && catalog.WallpaperAt(i)?.Id == item.Id) return false;
        return Buy(catalog, "wallpaper:" + item.Id, item.starCost);
    }
    public static bool TryBuyPuzzle(PuzzleCatalog catalog, int index)
    {
        if (index < 0 || index >= catalog.Count) return false;
        var item = catalog[index];
        if (item.starCost <= 0 || string.IsNullOrWhiteSpace(item.id)
            || (item.image == null && item.normalImage == null && item.hardImage == null)) return false;
        for (int i = 0; i < catalog.Count; i++)
            if (i != index && catalog[i].id == item.id) return false;
        return Buy(catalog, "puzzle:" + item.id, item.starCost);
    }
    private static bool Buy(PuzzleCatalog catalog, string item, int cost)
    {
        var account = Read(catalog.ProgressId);
        Synchronize(account, catalog.ProgressId, catalog.Count);
        if (account.purchases.Contains(item) || Balance(account) < cost) return false;
        account.spent += cost;
        account.purchases.Add(item);
        Save(catalog.ProgressId, account);
        return true;
    }
}
