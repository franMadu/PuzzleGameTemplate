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
        => Build(sequence, out _);

    public static List<Stage> Build(DialogueSequence sequence, out string appearanceError)
    {
        appearanceError = null;
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
        var outfits = new Dictionary<DialogueCharacter, string>();
        var expressions = new Dictionary<DialogueCharacter, string>();
        foreach (var actor in cast)
        {
            outfits[actor] = null;
            expressions[actor] = null;
        }
        foreach (var selection in sequence.InitialOutfits)
            if (selection != null && selection.character != null && outfits.ContainsKey(selection.character))
                outfits[selection.character] = selection.outfit;
        foreach (var selection in sequence.InitialExpressions)
            if (selection != null && selection.character != null && expressions.ContainsKey(selection.character))
                expressions[selection.character] = selection.expression;
        RefreshAppearances(sequence, cast, outfits, expressions, appearances, "inicio", ref appearanceError);
        DialogueCharacter left = null;
        DialogueCharacter right = null;
        if (!sequence.EnterOnFirstSpeech)
            foreach (var actor in cast)
            {
                if (appearances[actor].Portrait == null) continue;
                if (left == null) left = actor;
                else { right = actor; break; }
            }
        DialogueSpeakerSide previousSide = DialogueSpeakerSide.Narration;
        var lines = sequence.Lines;
        for (int index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!Valid(line)) continue;
            var actor = Resolve(line, cast);
            if (actor != null && !string.IsNullOrWhiteSpace(line.outfit))
                outfits[actor] = line.outfit;
            if (actor != null && !string.IsNullOrWhiteSpace(line.expression))
                expressions[actor] = line.expression;
            foreach (var reaction in line.reactions ?? Array.Empty<DialogueReaction>())
                if (reaction?.character != null && expressions.ContainsKey(reaction.character) && !string.IsNullOrWhiteSpace(reaction.expression))
                    expressions[reaction.character] = reaction.expression;
            RefreshAppearances(sequence, cast, outfits, expressions, appearances, "intervención " + (index + 1), ref appearanceError);
            // A voice without a sprite never reserves a visible slot or evicts a listener.
            // Recheck after outfit changes, which may add or remove an actor's portrait.
            if (left != null && appearances[left].Portrait == null) left = null;
            if (right != null && appearances[right].Portrait == null) right = null;
            DialogueSpeakerSide side = DialogueSpeakerSide.Narration;
            if (actor != null && appearances[actor].Portrait != null)
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

    private static void RefreshAppearances(DialogueSequence sequence, List<DialogueCharacter> cast,
        Dictionary<DialogueCharacter, string> outfits, Dictionary<DialogueCharacter, string> expressions,
        Dictionary<DialogueCharacter, DialogueAppearance> appearances, string location, ref string error)
    {
        foreach (var actor in cast)
        {
            bool valid = actor.TryGetAppearance(outfits[actor], expressions[actor], out var appearance);
            appearances[actor] = appearance;
            if (!valid && error == null)
                error = sequence.name + ": no existe la combinación outfit/expresión de " + actor.DisplayName
                    + " (" + (outfits[actor] ?? "default") + "/" + (expressions[actor] ?? "default") + ", " + location + "). Se usa el retrato de respaldo.";
        }
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
