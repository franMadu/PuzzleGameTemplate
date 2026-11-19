using System;
using UnityEngine;

public enum PuzzleDifficulty
{
    [InspectorName("Fácil · 3×3")] Easy,
    [InspectorName("Normal · 4×4")] Normal,
    [InspectorName("Difícil · 5×5")] Hard
}

[Serializable]
public class PuzzleChapter
{
    public string title;
    [TextArea] public string story;
    [InspectorName("Imagen · Fácil (3×3)")]
    public Texture2D image;
    [Tooltip("Imagen de la dificultad normal. Asigna una imagen diferente a la de fácil.")]
    [InspectorName("Imagen · Normal (4×4)")]
    public Texture2D normalImage;
    [Tooltip("Imagen de la dificultad difícil.")]
    [InspectorName("Imagen · Difícil (5×5)")]
    public Texture2D hardImage;
    public Texture2D ImageForDifficulty(int value) => value == 1 ? normalImage : value == 2 ? hardImage : image;
    [Tooltip("Dificultad de este puzzle en el modo historia. Al repetirlo, el jugador puede elegir otra.")]
    public PuzzleDifficulty difficulty;
    public int StoryDifficulty => Mathf.Clamp((int)difficulty, 0, 2);
    [Tooltip("Música del puzzle en sus tres dificultades. Vacío deja el puzzle sin música.")]
    public AudioClip music;
    public DialogueSequence introduction;
    public DialogueSequence conclusion;
}

[CreateAssetMenu(menuName = "One Hand Puzzle/Catalog")]
public class PuzzleCatalog : ScriptableObject
{
    [Tooltip("Identificador permanente de este juego. Cada juego de la serie debe tener uno distinto.")]
    [SerializeField] private string progressId = "OneHandPuzzle";
    [SerializeField] private string gameTitle = "ONE HAND\nPUZZLE";
    [SerializeField, TextArea(2, 4)] private string endingText = "Has completado el viaje.";
    [SerializeField] private StorySequence story;
    [SerializeField] private PuzzleChapter[] chapters;
    public string ProgressId => string.IsNullOrWhiteSpace(progressId) ? "OneHandPuzzle" : progressId;
    public string GameTitle => gameTitle;
    public string EndingText => endingText;
    public StorySequence Story => story;
    public int Count => chapters?.Length ?? 0;
    public PuzzleChapter this[int index] => chapters[index];
}
