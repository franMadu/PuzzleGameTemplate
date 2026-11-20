using System;
using System.Collections.Generic;
using UnityEngine;

public enum StoryStepKind { Conversation, Puzzle }

[Serializable]
public class StorySubchapter
{
    public string title;
    [Tooltip("Identificador del primer paso de este subcapítulo.")]
    public string firstStepId;
    [Tooltip("Identificador del último paso, incluido. Puede ser el mismo que el primero.")]
    public string lastStepId;
    [InspectorName("Fondos al completar el subcapítulo")]
    [Tooltip("Uno o varios fondos del catálogo que se ganan al terminar el último paso de este subcapítulo.")]
    public Texture2D[] wallpaperRewards;
}

[Serializable]
public class StoryChapter
{
    public string title;
    [InspectorName("Fondos al completar el capítulo")]
    [Tooltip("Uno o varios fondos del catálogo que se desbloquean al terminar todos los subcapítulos. Arrastra sus imágenes aquí.")]
    public Texture2D[] wallpaperRewards;
    public StorySubchapter[] subchapters;
}

[Serializable]
public class StoryStep
{
    [Tooltip("Identificador único y permanente. Se usa para reanudar la partida.")]
    public string id;
    public string title;
    public StoryStepKind kind;
    public DialogueSequence conversation;
    [Tooltip("Índice de la imagen dentro del catálogo, empezando por cero.")]
    [Min(0)] public int puzzleIndex;
}

[CreateAssetMenu(menuName = "One Hand Puzzle/Story Sequence")]
public class StorySequence : ScriptableObject
{
    [SerializeField] private StoryStep[] steps;
    [SerializeField] private StoryChapter[] chapters;
    public StoryChapter[] Chapters => chapters ?? Array.Empty<StoryChapter>();
    public int Count => steps?.Length ?? 0;
    public StoryStep this[int index] => steps[index];

    public int IndexOf(string id)
    {
        for (int i = 0; i < Count; i++)
            if (steps[i] != null && steps[i].id == id) return i;
        return -1;
    }

    public bool Validate(PuzzleCatalog catalog, out string error)
    {
        error = null;
        if (Count == 0) error = "La historia no tiene pasos.";
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; error == null && i < Count; i++)
        {
            var step = steps[i];
            if (step == null || string.IsNullOrWhiteSpace(step.id) || !ids.Add(step.id))
                error = $"El paso {i + 1} necesita un identificador único.";
            else if (step.kind == StoryStepKind.Conversation && step.conversation == null)
                error = $"Falta la conversación del paso {step.id}.";
            else if (step.kind == StoryStepKind.Puzzle && (step.puzzleIndex < 0 || step.puzzleIndex >= catalog.Count
                         || catalog[step.puzzleIndex].ImageForDifficulty(catalog[step.puzzleIndex].StoryDifficulty) == null))
                error = $"La imagen del paso {step.id} no existe en el catálogo.";
            else if (step.kind != StoryStepKind.Conversation && step.kind != StoryStepKind.Puzzle)
                error = $"El tipo del paso {step.id} no es válido.";
        }
        var covered = new HashSet<int>();
        foreach (var chapter in Chapters)
        {
            if (error != null) break;
            if (chapter == null || string.IsNullOrWhiteSpace(chapter.title) || chapter.subchapters == null)
            { error = "Cada capítulo necesita un título y subcapítulos."; break; }
            error = ValidateRewards(catalog, chapter.wallpaperRewards, chapter.title);
            if (error != null) break;
            foreach (var section in chapter.subchapters)
            {
                int first = section == null ? -1 : IndexOf(section.firstStepId);
                int last = section == null ? -1 : IndexOf(section.lastStepId);
                if (first < 0 || last < first || string.IsNullOrWhiteSpace(section.title))
                { error = "Subcapítulo con título o rango de pasos incorrecto."; break; }
                error = ValidateRewards(catalog, section.wallpaperRewards, section.title);
                if (error != null) break;
                int puzzles = 0;
                for (int i = first; i <= last; i++)
                {
                    if (!covered.Add(i)) { error = "Los subcapítulos no pueden compartir pasos."; break; }
                    if (steps[i].kind == StoryStepKind.Puzzle) puzzles++;
                }
                if (puzzles > 1) error = "Cada subcapítulo admite un puzzle como máximo.";
                if (error != null) break;
            }
        }
        return error == null;
    }

    private static string ValidateRewards(PuzzleCatalog catalog, Texture2D[] rewards, string title)
    {
        if (rewards == null) return null;
        foreach (var image in rewards)
        {
            if (image == null) continue;
            bool registered = false;
            for (int i = 0; i < catalog.WallpaperCount; i++)
                if (catalog.WallpaperAt(i)?.image == image) { registered = true; break; }
            if (!registered) return $"El fondo {image.name} de {title} debe añadirse a Fondos de pantalla del catálogo.";
        }
        return null;
    }

    public int PuzzleIn(StorySubchapter section)
    {
        for (int i = IndexOf(section.firstStepId); i >= 0 && i <= IndexOf(section.lastStepId); i++)
            if (steps[i].kind == StoryStepKind.Puzzle) return steps[i].puzzleIndex;
        return -1;
    }
}
