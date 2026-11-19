using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzleTile : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int Cell { get; set; }
    public int CorrectCell { get; private set; }
    public RectTransform Rect { get; private set; }
    private Puzzle puzzle;
    private bool dragged;
    private bool canDrag;
    private Vector2 pointerStart;
    private Vector2 origin;
    private Vector2 destination;

    public void Initialize(Puzzle owner, int correctCell)
    {
        puzzle = owner;
        CorrectCell = correctCell;
        Rect = (RectTransform)transform;
    }

    public void OnPointerDown(PointerEventData eventData) => dragged = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!dragged && eventData.button == PointerEventData.InputButton.Left)
            puzzle.TryMove(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragged = true;
        canDrag = eventData.button == PointerEventData.InputButton.Left && puzzle.BeginDrag(this);
        if (!canDrag) return;
        origin = puzzle.PositionFor(Cell);
        GameAudio.DragTile();
        destination = puzzle.EmptyPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent,
            eventData.position, eventData.pressEventCamera, out pointerStart);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent,
            eventData.position, eventData.pressEventCamera, out var pointer);
        Vector2 direction = destination - origin;
        float progress = Mathf.Clamp01(Vector2.Dot(pointer - pointerStart, direction) / direction.sqrMagnitude);
        Vector2 position = Vector2.Lerp(origin, destination, progress);
        if ((position - Rect.anchoredPosition).sqrMagnitude > 0.25f) GameAudio.DragTile();
        Rect.anchoredPosition = position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        canDrag = false;
        puzzle.EndDrag(this);
        if (Vector2.Distance(origin, Rect.anchoredPosition) >= Vector2.Distance(origin, destination) * 0.25f)
            puzzle.TryMove(this);
        else
            puzzle.ReturnTile(this);
    }

    private void OnDisable()
    {
        if (!canDrag || puzzle == null) return;
        canDrag = false;
        puzzle.EndDrag(this);
        Rect.anchoredPosition = puzzle.PositionFor(Cell);
    }
}
