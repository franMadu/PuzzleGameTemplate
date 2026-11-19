using System;
using System.Collections.Generic;

// Resolve the two visible slots before pagination, so long lines do not change casting decisions.
public static class DialogueStagePlanner
{
    public sealed class Stage
    {
        public DialogueLine Line { get; internal set; }
        public DialogueCharacter Speaker { get; internal set; }
        public DialogueCharacter Left { get; internal set; }
        public DialogueCharacter Right { get; internal set; }
        public DialogueAppearance LeftAppearance { get; internal set; }
        public DialogueAppearance RightAppearance { get; internal set; }
        public DialogueSpeakerSide SpeakingSide { get; internal set; }
    }

    public static List<Stage> Build(DialogueSequence sequence)
    {
        var result = new List<Stage>();
        if (sequence == null) return result;
        var cast = new List<DialogueCharacter>();
        foreach (var actor in sequence.Characters)
            if (actor != null && !cast.Contains(actor)) cast.Add(actor);
        foreach (var line in sequence.Lines)
            if (Valid(line) && line.character != null && !cast.Contains(line.character)) cast.Add(line.character);
        // Conversations authored with the original left/right sprite fields retain their behavior.
        if (cast.Count == 0) return result;
        var appearances = new Dictionary<DialogueCharacter, DialogueAppearance>();
        foreach (var actor in cast)
        {
            actor.TryGetAppearance(null, out var appearance);
            appearances[actor] = appearance;
        }
        foreach (var selection in sequence.InitialOutfits)
            if (selection != null && selection.character != null && appearances.ContainsKey(selection.character))
            {
                selection.character.TryGetAppearance(selection.outfit, out var appearance);
                appearances[selection.character] = appearance;
            }
        DialogueCharacter left = cast[0];
        DialogueCharacter right = cast.Count > 1 ? cast[1] : null;
        DialogueSpeakerSide previousSide = DialogueSpeakerSide.Narration;
        var lines = sequence.Lines;
        for (int index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!Valid(line)) continue;
            var actor = Resolve(line, cast);
            if (actor != null && !string.IsNullOrWhiteSpace(line.outfit))
            {
                actor.TryGetAppearance(line.outfit, out var appearance);
                appearances[actor] = appearance;
            }
            DialogueSpeakerSide side = DialogueSpeakerSide.Narration;
            if (actor != null)
            {
                if (actor == left) side = DialogueSpeakerSide.Left;
                else if (actor == right) side = DialogueSpeakerSide.Right;
                else
                {
                    bool replaceLeft;
                    if (left == null) replaceLeft = true;
                    else if (right == null) replaceLeft = false;
                    else
                    {
                        int nextLeft = NextUse(lines, index + 1, left, cast);
                        int nextRight = NextUse(lines, index + 1, right, cast);
                        // Never speaking again counts as latest. For equal futures, replace the listener.
                        replaceLeft = nextLeft == nextRight ? previousSide != DialogueSpeakerSide.Left : nextLeft > nextRight;
                    }
                    if (replaceLeft) { left = actor; side = DialogueSpeakerSide.Left; }
                    else { right = actor; side = DialogueSpeakerSide.Right; }
                }
            }
            result.Add(new Stage { Line = line, Speaker = actor, Left = left, Right = right, SpeakingSide = side,
                LeftAppearance = left != null ? appearances[left] : default,
                RightAppearance = right != null ? appearances[right] : default });
            if (side != DialogueSpeakerSide.Narration) previousSide = side;
        }
        return result;
    }

    private static bool Valid(DialogueLine line) => line != null && !string.IsNullOrWhiteSpace(line.text);

    private static DialogueCharacter Resolve(DialogueLine line, List<DialogueCharacter> cast)
    {
        if (line.character != null) return line.character;
        if (line.speakerSide == DialogueSpeakerSide.Narration || string.IsNullOrWhiteSpace(line.speaker)) return null;
        foreach (var actor in cast)
            if (string.Equals(actor.DisplayName, line.speaker.Trim(), StringComparison.OrdinalIgnoreCase)) return actor;
        return null;
    }

    private static int NextUse(DialogueLine[] lines, int from, DialogueCharacter actor, List<DialogueCharacter> cast)
    {
        for (int i = from; i < lines.Length; i++)
            if (Valid(lines[i]) && Resolve(lines[i], cast) == actor) return i;
        return int.MaxValue;
    }
}
