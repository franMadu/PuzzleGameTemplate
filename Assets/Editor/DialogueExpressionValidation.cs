using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DialogueExpressionValidation
{
    private static readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();

    [MenuItem("Tools/One Hand Puzzle/Validate Dialogue Expressions")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs");
        try
        {
            var texture = Keep(new Texture2D(2, 2));
            Sprite Sprite() => Keep(UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one / 2));
            ValidateExpressionChoices(Sprite());
            var neutral = Sprite(); var happy = Sprite(); var sad = Sprite(); var angry = Sprite();
            var travel = Sprite(); var travelHappy = Sprite(); var travelSad = Sprite(); var gala = Sprite();
            var a = Actor("A", neutral, Expression("feliz", happy), Expression("triste", sad));
            var b = Actor("B", neutral, Expression("enfadado", angry));
            var c = Actor("C", neutral);
            var voice = Actor("Sin retrato", null);
            Set(a, "outfits", new[] {
                new DialogueOutfit { id = "viaje", portrait = travel, facing = DialogueFacing.Left,
                    expressions = new[] { Expression("feliz", travelHappy), Expression("triste", travelSad) } },
                new DialogueOutfit { id = "gala", portrait = gala }
            });
            Require(a.ValidateExpressions(out _), "Expression IDs may repeat across different outfits");
            Require(a.TryGetAppearance("viaje", "TRISTE", out var appearance) && appearance.Portrait == travelSad && appearance.FacesLeft, "Expression lookup respects outfit and orientation");

            var scenario = Sequence(new[] { a, b }, new[] {
                Line(a, "feliz"),
                Line(b, "enfadado", new DialogueReaction { character = a, expression = "triste" }),
                Line(b),
                new DialogueLine { character = a, outfit = "viaje", text = "Conservo la expresión al cambiarme." },
                Line(a, DialogueCharacter.DefaultExpressionId),
                new DialogueLine { character = a, outfit = DialogueCharacter.DefaultOutfitId, text = "Vuelvo a la ropa inicial." }
            });
            Require(scenario.ValidateAppearances(out string error), error);
            var stages = DialogueStagePlanner.Build(scenario);
            Require(stages[0].LeftAppearance.Portrait == happy, "A starts happy");
            Require(stages[1].LeftAppearance.Portrait == sad && stages[1].RightAppearance.Portrait == angry, "Speaker becomes angry while listener becomes sad on the same line");
            Require(stages[2].LeftAppearance.Portrait == sad && stages[2].RightAppearance.Portrait == angry, "Both expressions persist");
            Require(stages[3].LeftAppearance.Portrait == travelSad, "Changing outfit preserves expression ID using that outfit's sprite");
            Require(stages[4].LeftAppearance.Portrait == travel, "Expression reset retains outfit");
            Require(stages[5].LeftAppearance.Portrait == neutral, "Independent outfit reset");
            Require(stages[0].LeftAppearance.Portrait == happy, "Earlier snapshots remain unchanged");

            var initial = Sequence(new[] { a, b }, new[] { Line(b) });
            Set(initial, "initialOutfits", new[] { new DialogueOutfitSelection { character = a, outfit = "viaje" } });
            Set(initial, "initialExpressions", new[] {
                new DialogueExpressionSelection { character = a, expression = "feliz" },
                new DialogueExpressionSelection { character = b, expression = "enfadado" }
            });
            Require(initial.ValidateAppearances(out error), error);
            Require(DialogueStagePlanner.Build(initial)[0].LeftAppearance.Portrait == travelHappy, "Initial expressions use initial outfits before the first turn");

            var returning = Sequence(new[] { a, b, c }, new[] {
                Line(a, "feliz"), Line(b), Line(c),
                new DialogueLine { text = "Mientras tanto...", speakerSide = DialogueSpeakerSide.Narration,
                    reactions = new[] { new DialogueReaction { character = a, expression = "triste" } } },
                Line(b), Line(a)
            });
            Require(returning.ValidateAppearances(out error), error);
            stages = DialogueStagePlanner.Build(returning);
            Require(stages[2].Left == c && stages[3].Left == c && stages[5].Left == a && stages[5].LeftAppearance.Portrait == sad, "Offscreen reaction persists without forcing entrance");
            Require(DialogueStagePlanner.Build(scenario)[0].LeftAppearance.Portrait == happy, "Replaying resets expression changes");
            Require(DialogueStagePlanner.Build(Sequence(new[] { a }, new[] { Line(a) }))[0].LeftAppearance.Portrait == neutral, "No expression leaks between conversations");
            Require(a.Portrait == neutral, "Character asset is not mutated");
            Require(Sequence(new[] { a, voice }, new[] { Line(voice) }).ValidateAppearances(out error), "Portraitless speakers remain supported: " + error);

            Require(!a.TryGetAppearance("gala", "triste", out appearance) && appearance.Portrait == gala, "Missing combination falls back within the selected outfit");
            var missing = Sequence(new[] { a }, new[] { Line(a, "triste"),
                new DialogueLine { character = a, outfit = "gala", text = "Falta este sprite." },
                new DialogueLine { character = a, outfit = "viaje", text = "La expresión sigue guardada." }
            });
            Require(!missing.ValidateAppearances(out _), "Missing outfit/expression combination is reported");
            stages = DialogueStagePlanner.Build(missing);
            Require(stages[1].LeftAppearance.Portrait == gala && stages[2].LeftAppearance.Portrait == travelSad, "Fallback preserves expression state for later compatible outfits");
            var combinedReset = Sequence(new[] { a }, new[] { Line(a, "triste"),
                new DialogueLine { character = a, outfit = "gala", expression = "default", text = "Cambio ambas cosas a la vez." }
            });
            Require(combinedReset.ValidateAppearances(out error), "Outfit and expression changes are applied atomically: " + error);
            Require(!Sequence(new[] { a }, new[] { Line(a, "inexistente") }).ValidateAppearances(out _), "Unknown expression rejected");
            Require(!Sequence(new[] { a, b }, new[] { Line(a, "feliz", new DialogueReaction { character = a, expression = "triste" }) }).ValidateAppearances(out _), "Conflicting changes rejected");
            Require(!Sequence(new[] { a }, new[] { Line(a, null, new DialogueReaction { character = b, expression = "enfadado" }) }).ValidateAppearances(out _), "Reaction target must belong to cast");
            var duplicateInitial = Sequence(new[] { a }, new[] { Line(a) });
            Set(duplicateInitial, "initialExpressions", new[] {
                new DialogueExpressionSelection { character = a, expression = "feliz" },
                new DialogueExpressionSelection { character = a, expression = "triste" }
            });
            Require(!duplicateInitial.ValidateAppearances(out _), "Duplicate initial expression rejected");
            Set(c, "expressions", new[] { Expression("feliz", happy), Expression("FELIZ", sad) });
            Require(!c.ValidateExpressions(out _), "Duplicate expression IDs rejected within an outfit");
            Set(c, "expressions", new[] { Expression("default", happy) });
            Require(!c.ValidateExpressions(out _), "Reserved expression ID rejected");
            Set(c, "expressions", new[] { Expression("triste", null) });
            Require(!c.ValidateExpressions(out _), "Missing expression sprite rejected");

            ValidateController(a, b, happy, sad, angry);
            foreach (string guid in AssetDatabase.FindAssets("t:DialogueSequence", new[] { "Assets" }))
            {
                var existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetDatabase.GUIDToAssetPath(guid));
                Require(existing.ValidateAppearances(out error), error);
            }
            string result = "PASS: simultaneous speaker/listener expressions, initial states, persistence, outfits, resets, narration/offscreen reactions, reentry, replay isolation, fallback/validation, sprite updates, pagination and existing assets.";
            File.WriteAllText("Logs/DialogueExpressionValidation.result", result);
            Debug.Log(result);
        }
        catch (Exception error)
        {
            File.WriteAllText("Logs/DialogueExpressionValidation.result", "FAIL: " + error);
            throw;
        }
        finally
        {
            for (int i = temporary.Count - 1; i >= 0; i--) if (temporary[i] != null) UnityEngine.Object.DestroyImmediate(temporary[i]);
            temporary.Clear();
        }
    }

    private static void ValidateController(DialogueCharacter a, DialogueCharacter b, Sprite happy, Sprite sad, Sprite angry)
    {
        var root = Keep(new GameObject("Expression validation"));
        var controller = root.AddComponent<DialogueController>();
        foreach (bool right in new[] { false, true })
        {
            var imageRoot = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            imageRoot.transform.SetParent(root.transform);
            var image = imageRoot.GetComponent<Image>();
            var actor = right ? b : a;
            var focus = Field(controller, right ? "rightFocus" : "leftFocus");
            Invoke(controller, "SetCharacter", image, happy, actor, false, !right, right, focus, true);
            Invoke(controller, "SetCharacter", image, right ? angry : sad, actor, false, right, right, focus, false);
            Require(image.sprite == (right ? angry : sad) && Field(focus, "motion").ToString() == "Focus", "Expression update does not trigger an exit/entrance");
        }
        var body = new GameObject("Body", typeof(RectTransform), typeof(Text));
        body.transform.SetParent(root.transform);
        var text = body.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 23;
        text.rectTransform.sizeDelta = new Vector2(240, 80);
        Set(controller, "bodyText", text);
        var page = Activator.CreateInstance(typeof(DialogueController).GetNestedType("Page", BindingFlags.NonPublic), true);
        Set(page, "text", string.Join(" ", Enumerable.Repeat("Un personaje habla y el otro reacciona.", 15)));
        Set(page, "leftActor", a); Set(page, "rightActor", b);
        Set(page, "leftCharacter", sad); Set(page, "rightCharacter", angry);
        Invoke(controller, "AddPages", page);
        var pages = (IList)Field(controller, "pages");
        Require(pages.Count > 1, "Long line is paginated");
        foreach (object fragment in pages)
            Require((Sprite)Field(fragment, "leftCharacter") == sad && (Sprite)Field(fragment, "rightCharacter") == angry, "Every page keeps both expressions");
    }

    private static void ValidateExpressionChoices(Sprite sprite)
    {
        var actor = Actor("Outfit choices", sprite, Expression("seria", sprite), Expression("simpatica", sprite));
        var listener = Actor("Listener choices", sprite, Expression("neutral", sprite));
        Set(actor, "outfits", new[] {
            new DialogueOutfit { id = "baloncesto", portrait = sprite,
                expressions = new[] { Expression("bostezando", sprite), Expression("aburrimiento", sprite) } },
            new DialogueOutfit { id = "sin_expresiones", portrait = sprite }
        });
        var sequence = Sequence(new[] { actor, listener }, new[] {
            Line(actor), Line(listener, null, new DialogueReaction { character = actor }),
            new DialogueLine { character = actor, outfit = "default" },
            Line(actor, null, new DialogueReaction { character = actor }, new DialogueReaction { character = listener }),
            Line(actor)
        });
        Set(sequence, "initialOutfits", new[] { new DialogueOutfitSelection { character = actor, outfit = "baloncesto" } });
        Set(sequence, "initialExpressions", new[] { new DialogueExpressionSelection { character = actor } });
        using (var serialized = new SerializedObject(sequence))
        {
            void Choices(string path, DialogueCharacter target, params string[] expected)
            {
                var actual = DialogueExpressionIdDrawer.AvailableExpressionIds(serialized.FindProperty(path), target).ToArray();
                Require(actual.SequenceEqual(expected), "Outfit expression choices at " + path + ": " + string.Join(", ", actual));
            }
            Choices("initialExpressions.Array.data[0].expression", actor, "bostezando", "aburrimiento");
            Choices("lines.Array.data[0].expression", actor, "bostezando", "aburrimiento");
            Choices("lines.Array.data[1].reactions.Array.data[0].expression", actor, "bostezando", "aburrimiento");
            Choices("lines.Array.data[2].expression", actor, "seria", "simpatica");
            // Pending Inspector edits must be visible before ApplyModifiedProperties.
            serialized.FindProperty("lines.Array.data[2].outfit").stringValue = "sin_expresiones";
            Choices("lines.Array.data[2].expression", actor);
            Choices("lines.Array.data[3].expression", actor);
            serialized.FindProperty("lines.Array.data[3].outfit").stringValue = "baloncesto";
            Choices("lines.Array.data[3].reactions.Array.data[0].expression", actor, "bostezando", "aburrimiento");
            Choices("lines.Array.data[3].reactions.Array.data[1].expression", listener, "neutral");
            Choices("lines.Array.data[4].expression", actor, "bostezando", "aburrimiento");
            serialized.FindProperty("lines.Array.data[3].outfit").stringValue = "no_existe";
            Choices("lines.Array.data[4].expression", actor);
            serialized.FindProperty("initialOutfits.Array.data[0].outfit").stringValue = "default";
            Choices("initialExpressions.Array.data[0].expression", actor, "seria", "simpatica");
        }
    }

    private static DialogueExpression Expression(string id, Sprite sprite) => new DialogueExpression { id = id, portrait = sprite };
    private static DialogueCharacter Actor(string name, Sprite portrait, params DialogueExpression[] expressions)
    {
        var actor = Keep(ScriptableObject.CreateInstance<DialogueCharacter>());
        actor.name = name; Set(actor, "portrait", portrait); Set(actor, "expressions", expressions);
        return actor;
    }
    private static DialogueLine Line(DialogueCharacter actor, string expression = null, params DialogueReaction[] reactions)
        => new DialogueLine { character = actor, expression = expression, reactions = reactions, text = "Texto de prueba." };
    private static DialogueSequence Sequence(DialogueCharacter[] actors, DialogueLine[] lines)
    {
        var sequence = Keep(ScriptableObject.CreateInstance<DialogueSequence>());
        Set(sequence, "characters", actors); Set(sequence, "lines", lines);
        return sequence;
    }
    private static T Keep<T>(T value) where T : UnityEngine.Object { temporary.Add(value); return value; }
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
