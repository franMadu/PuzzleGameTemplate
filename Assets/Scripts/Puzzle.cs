using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Puzzle : MonoBehaviour
{
    public int Size { get; private set; } = 3;
    public int Moves => moves;
    public int MinimumMoves { get; private set; }
    public PuzzleGrade Grade => PuzzleRating.GradeFor(moves, MinimumMoves);
    public bool IsRevealingCompletion { get; private set; }
    public event System.Action Completed;
    [SerializeField] private Texture2D puzzleImage;
    [SerializeField] private RectTransform board;
    [SerializeField] private PuzzleTile[] tiles;
    [SerializeField] private Text status;
    [SerializeField, Min(0.01f)] private float slideDuration = 0.16f;
    [SerializeField, Min(0)] private float completionFlashInDuration = 1f;
    [SerializeField, Min(0)] private float completionFlashOutDuration = 1.1f;
    [SerializeField, Min(0)] private float completedImageHoldDuration = 0.9f;
    [SerializeField] private RawImage completedImage;
    [SerializeField] private Image completionFlash;
    private PuzzleTile[] cells;
    private bool initialized;
    private int emptyCell = 8;
    private int moves;
    private bool busy;
    private bool solved;
    private bool inputBlocked;
    private PuzzleTile draggedTile;
    private PuzzleShuffle.Deal initialDeal;
#if UNITY_EDITOR
    private PuzzleGodModeHints godHints;
#endif

    public bool CanMove(PuzzleTile tile) => !ScreenTransition.IsBusy && !inputBlocked && !busy && !solved && tile != null &&
        (draggedTile == null || draggedTile == tile) &&
        Mathf.Abs(tile.Cell / Size - emptyCell / Size) +
        Mathf.Abs(tile.Cell % Size - emptyCell % Size) == 1;

    private void Start()
    {
        if (!initialized) Configure(puzzleImage, 3);
    }

    public void Configure(Texture2D texture, int size)
    {
        Suspend();
        initialized = true;
        puzzleImage = texture;
        Size = Mathf.Clamp(size, 3, 5);
        int count = Size * Size - 1;
        if (tiles.Length < count)
        {
            int previousCount = tiles.Length;
            System.Array.Resize(ref tiles, count);
            for (int i = previousCount; i < count; i++)
                tiles[i] = Instantiate(tiles[0], board);
        }
        cells = new PuzzleTile[Size * Size];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].gameObject.SetActive(i < count);
            if (i >= count) continue;
            tiles[i].name = $"Tile {i + 1}";
            tiles[i].Initialize(this, i);
            tiles[i].Rect.sizeDelta = Vector2.one * (board.rect.width / Size - 4);
            var image = tiles[i].GetComponent<RawImage>();
            image.texture = puzzleImage;
            // Crop to a central square. UI rows run downwards; UV coordinates run upwards.
            float side = Mathf.Min(puzzleImage.width, puzzleImage.height);
            float width = side / puzzleImage.width;
            float height = side / puzzleImage.height;
            image.uvRect = new Rect((1 - width) / 2 + i % Size * width / Size,
                (1 - height) / 2 + (Size - 1 - i / Size) * height / Size,
                width / Size, height / Size);
        }
        ShuffleBoard();
    }

    public void Shuffle()
    {
        if (ScreenTransition.IsBusy || IsRevealingCompletion || inputBlocked || busy || draggedTile != null || !initialized) return;
        ScreenTransition.Change(ShuffleBoard);
    }

    private void ShuffleBoard()
    {
        if (inputBlocked || busy || draggedTile != null) return;
        if (!initialized) return;
        ResetCompletionVisuals();
        initialDeal = PuzzleShuffle.Create(Size, Random.Range(0, int.MaxValue));
        MinimumMoves = initialDeal.MinimumMoves;
        System.Array.Clear(cells, 0, cells.Length);
        emptyCell = cells.Length - 1;
        moves = 0;
        solved = false;
        for (int cell = 0; cell < initialDeal.Tiles.Length; cell++)
        {
            int index = initialDeal.Tiles[cell];
            if (index < 0) continue;
            var tile = tiles[index];
            cells[cell] = tile;
            tile.Cell = cell;
            tile.Rect.anchoredPosition = PositionFor(cell);
        }
        UpdateStatus();
#if UNITY_EDITOR
        if (godHints == null) godHints = gameObject.AddComponent<PuzzleGodModeHints>();
        godHints.ResetBoard(this, cells, initialDeal.Solution);
#endif
    }

    public Vector2 PositionFor(int cell)
    {
        float step = board.rect.width / Size;
        float center = (Size - 1) / 2f;
        return new Vector2((cell % Size - center) * step, (center - cell / Size) * step);
    }

    public void SetInputBlocked(bool blocked) => inputBlocked = blocked;

    public void Suspend()
    {
#if UNITY_EDITOR
        if (godHints != null) godHints.SuspendHints();
#endif
        StopAllCoroutines();
        IsRevealingCompletion = false;
        busy = false;
        draggedTile = null;
    }

    public Vector2 EmptyPosition => PositionFor(emptyCell);
    public bool BeginDrag(PuzzleTile tile)
    {
        if (!CanMove(tile)) return false;
        draggedTile = tile;
        return true;
    }

    public void EndDrag(PuzzleTile tile)
    {
        if (draggedTile == tile) draggedTile = null;
    }

    public void TryMove(PuzzleTile tile)
    {
        if (CanMove(tile)) StartCoroutine(Slide(tile, true));
    }

    public void ReturnTile(PuzzleTile tile)
    {
        if (!busy) StartCoroutine(Slide(tile, false));
    }

    private IEnumerator Slide(PuzzleTile tile, bool commit)
    {
        busy = true;
        Vector2 from = tile.Rect.anchoredPosition;
        Vector2 to = PositionFor(commit ? emptyCell : tile.Cell);
        if ((to - from).sqrMagnitude > 0.25f) GameAudio.DragTile();
        float elapsed = 0;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            tile.Rect.anchoredPosition = Vector2.Lerp(from, to,
                Mathf.SmoothStep(0, 1, elapsed / slideDuration));
            yield return null;
        }
        tile.Rect.anchoredPosition = to;
        if (commit)
        {
            cells[emptyCell] = tile;
            cells[tile.Cell] = null;
            int previousCell = tile.Cell;
            tile.Cell = emptyCell;
            emptyCell = previousCell;
            moves++;
#if UNITY_EDITOR
            if (godHints != null) godHints.CommittedMove(previousCell, tile.Cell);
#endif
            GameAudio.PlaceTile();
            solved = emptyCell == cells.Length - 1;
            for (int i = 0; i < cells.Length - 1 && solved; i++)
                solved = cells[i] != null && cells[i].CorrectCell == i;
            UpdateStatus();
        }
        busy = false;
        if (commit && solved)
        {
            StartCoroutine(RevealCompletedImage());
            Completed?.Invoke();
        }
    }

    private IEnumerator RevealCompletedImage()
    {
        IsRevealingCompletion = true;
        completedImage.raycastTarget = false;
        completedImage.texture = puzzleImage;
        float side = Mathf.Min(puzzleImage.width, puzzleImage.height);
        float width = side / puzzleImage.width, height = side / puzzleImage.height;
        completedImage.uvRect = new Rect((1 - width) / 2, (1 - height) / 2, width, height);
        completedImage.gameObject.SetActive(false);
        completedImage.transform.SetAsLastSibling();
        completionFlash.raycastTarget = false;
        completionFlash.color = new Color(1, 1, 1, 0);
        completionFlash.gameObject.SetActive(true);
        completionFlash.transform.SetAsLastSibling();
        yield return FadeCompletionFlash(0, 1, completionFlashInDuration);
        // Replace the grid while the white layer fully covers it, including the empty cell.
        completedImage.gameObject.SetActive(true);
        yield return null;
        yield return FadeCompletionFlash(1, 0, completionFlashOutDuration);
        completionFlash.gameObject.SetActive(false);
        if (completedImageHoldDuration > 0) yield return new WaitForSecondsRealtime(completedImageHoldDuration);
        IsRevealingCompletion = false;
    }

    private IEnumerator FadeCompletionFlash(float from, float to, float duration)
    {
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            completionFlash.color = new Color(1, 1, 1, Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / duration)));
            yield return null;
        }
        completionFlash.color = new Color(1, 1, 1, to);
    }

    private void ResetCompletionVisuals()
    {
        IsRevealingCompletion = false;
        if (completedImage != null) completedImage.gameObject.SetActive(false);
        if (completionFlash != null) completionFlash.gameObject.SetActive(false);
    }

    private void UpdateStatus()
    {
        CanvasText.Set(status, 0, moves, MinimumMoves);
    }
}
