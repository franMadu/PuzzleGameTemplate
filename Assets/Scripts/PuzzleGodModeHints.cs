#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// Entire helper is excluded from players, including development builds.
public class PuzzleGodModeHints : MonoBehaviour
{
    private Puzzle puzzle;
    private PuzzleTile[] cells;
    private List<int> route;
    private bool optimal;
    private bool previouslyEnabled;
    private CancellationTokenSource cancellation;
    private Task<int[]> search;
    private PuzzleHintGraphic marker;

    public void ResetBoard(Puzzle owner, PuzzleTile[] board, int[] solution)
    {
        CancelSearch();
        puzzle = owner;
        cells = board;
        route = new List<int>(solution);
        optimal = true; // The shuffle provides a proven minimum-length solution.
        HideMarker();
    }

    public void CommittedMove(int source, int destination)
    {
        CancelSearch();
        if (route.Count > 0 && route[0] == source) route.RemoveAt(0);
        else
        {
            // Undoing this move followed by the previous route is a valid upper bound.
            route.Insert(0, destination);
            optimal = false;
        }
        HideMarker();
    }

    private void Update()
    {
        bool enabled = GamePreferences.GodMode;
        if (!enabled || puzzle == null || route == null || route.Count == 0)
        {
            if (previouslyEnabled) CancelSearch();
            previouslyEnabled = enabled;
            HideMarker();
            return;
        }
        previouslyEnabled = true;
        if (!optimal)
        {
            HideMarker();
            if (search == null)
            {
                var state = new int[cells.Length];
                for (int i = 0; i < cells.Length; i++) state[i] = cells[i] == null ? -1 : cells[i].CorrectCell;
                int size = puzzle.Size;
                var upperBound = route.ToArray();
                cancellation = new CancellationTokenSource();
                var token = cancellation.Token;
                search = Task.Run(() => PuzzleHintSolver.Solve(state, size, upperBound, token), token);
            }
            if (!search.IsCompleted) return;
            if (search.Status == TaskStatus.RanToCompletion)
            {
                route = new List<int>(search.Result);
                optimal = true;
            }
            else if (search.IsFaulted) Debug.LogException(search.Exception, this);
            CancelSearch();
            return;
        }
        var tile = cells[route[0]];
        if (tile == null || !puzzle.CanMove(tile)) { HideMarker(); return; }
        if (marker == null)
        {
            var rect = new GameObject("God Mode Hint", typeof(RectTransform), typeof(CanvasRenderer)).GetComponent<RectTransform>();
            marker = rect.gameObject.AddComponent<PuzzleHintGraphic>();
            marker.raycastTarget = false;
        }
        var markerRect = marker.rectTransform;
        marker.gameObject.layer = tile.gameObject.layer;
        if (markerRect.parent != tile.Rect) markerRect.SetParent(tile.Rect, false);
        markerRect.SetAsLastSibling();
        markerRect.anchorMin = Vector2.zero;
        markerRect.anchorMax = Vector2.one;
        markerRect.offsetMin = markerRect.offsetMax = Vector2.zero;
        marker.SetDirection(puzzle.EmptyPosition - puzzle.PositionFor(tile.Cell));
        marker.color = new Color(1, 0.8f, 0.12f, 0.925f + 0.075f * Mathf.Sin(Time.unscaledTime * 5));
        marker.gameObject.SetActive(true);
    }

    private void HideMarker()
    {
        if (marker == null) return;
        marker.gameObject.SetActive(false);
    }
    public void SuspendHints()
    {
        CancelSearch();
        HideMarker();
        // Keep it out of tile prefabs when Configure clones tiles for a larger board.
        // Reparent only during explicit suspension, never inside activation callbacks.
        if (marker != null && marker.transform.parent != transform)
            marker.rectTransform.SetParent(transform, false);
    }
    private void CancelSearch()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
        search = null;
    }
    private void OnDisable() { CancelSearch(); HideMarker(); }
    private void OnDestroy() { CancelSearch(); if (marker != null) Destroy(marker.gameObject); }
}

[RequireComponent(typeof(CanvasRenderer))]
public class PuzzleHintGraphic : MaskableGraphic
{
    private Vector2 direction = Vector2.right;
    public void SetDirection(Vector2 value)
    {
        value.Normalize();
        if (direction == value) return;
        direction = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var r = rectTransform.rect;
        float thickness = Mathf.Min(9, r.width * 0.065f);
        Quad(mesh, r.xMin, r.yMin, r.xMax, r.yMin + thickness);
        Quad(mesh, r.xMin, r.yMax - thickness, r.xMax, r.yMax);
        Quad(mesh, r.xMin, r.yMin + thickness, r.xMin + thickness, r.yMax - thickness);
        Quad(mesh, r.xMax - thickness, r.yMin + thickness, r.xMax, r.yMax - thickness);
        float size = Mathf.Min(r.width, r.height);
        Vector2 center = r.center;
        Vector2 normal = new Vector2(-direction.y, direction.x);
        Vector2 tail = center - direction * size * 0.2f;
        Vector2 tip = center + direction * size * 0.22f;
        Vector2 head = center + direction * size * 0.04f;
        // A dark outline keeps the arrow readable on both light sand and dark landscapes.
        foreach (bool outline in new[] { true, false })
        {
            Color tint = outline ? new Color(0.06f, 0.08f, 0.09f, 0.95f) : color;
            float width = size * (outline ? 0.095f : 0.045f);
            Line(mesh, tail, tip, width, tint);
            Line(mesh, head + normal * size * 0.16f, tip, width, tint);
            Line(mesh, head - normal * size * 0.16f, tip, width, tint);
        }
    }
    private static void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color tint)
    {
        Vector2 normal = new Vector2(-(to - from).y, (to - from).x).normalized * width * 0.5f;
        int first = mesh.currentVertCount;
        mesh.AddVert(from - normal, tint, Vector2.zero);
        mesh.AddVert(from + normal, tint, Vector2.zero);
        mesh.AddVert(to + normal, tint, Vector2.zero);
        mesh.AddVert(to - normal, tint, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
    private void Quad(VertexHelper mesh, float left, float bottom, float right, float top)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
        mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
        mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
        mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
#endif
