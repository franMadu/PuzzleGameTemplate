using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DialogueOutfitValidation
{
    private static readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();

    [MenuItem("Tools/One Hand Puzzle/Validate Dialogue Outfits")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs");
        try
        {
            var texture = Keep(new Texture2D(2, 2));
            Sprite Sprite() => Keep(UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one / 2));
            var basic = Sprite();
            var travel = Sprite();
            var gala = Sprite();
            var a = Actor("A", basic);
            var b = Actor("B", basic);
            var c = Actor("C", basic);
            var outfits = new[] {
                new DialogueOutfit { id = "viaje", portrait = travel, facing = DialogueFacing.Left },
                new DialogueOutfit { id = "gala", portrait = gala, facing = DialogueFacing.Right }
            };
            Set(a, "outfits", outfits);
            Set(b, "outfits", outfits);
            Require(a.ValidateOutfits(out _), "Valid outfit list");
            Require(a.TryGetAppearance("VIAJE", out var selected) && selected.Portrait == travel && selected.FacesLeft, "Outfit lookup and facing");
            Require(!a.TryGetAppearance("missing", out var missing) && missing.Portrait == basic, "Missing outfit fallback");

            var sequence = Sequence(new[] { a, b }, new[] {
                Line(b), Line(a, "gala"), Line(a), Line(a, DialogueCharacter.DefaultOutfitId), Line(a)
            }, new[] {
                new DialogueOutfitSelection { character = a, outfit = "viaje" },
                new DialogueOutfitSelection { character = b, outfit = "gala" }
            });
            Require(sequence.ValidateOutfits(out _), "Valid conversation outfits");
            var stages = DialogueStagePlanner.Build(sequence);
            Require(stages[0].LeftAppearance.Portrait == travel && stages[0].LeftAppearance.FacesLeft, "Initial outfit before first turn");
            Require(stages[0].RightAppearance.Portrait == gala, "Initial listener/speaker outfits");
            Require(stages[1].LeftAppearance.Portrait == gala && stages[2].LeftAppearance.Portrait == gala, "Line change persists");
            Require(stages[3].LeftAppearance.Portrait == basic && stages[4].LeftAppearance.Portrait == basic, "Explicit default reset persists");
            Require(stages[0].LeftAppearance.Portrait == travel, "Earlier stage snapshots remain unchanged");
            Require(DialogueStagePlanner.Build(sequence)[0].LeftAppearance.Portrait == travel, "Replay resets initial outfit");

            var returning = Sequence(new[] { a, b, c }, new[] { Line(a, "viaje"), Line(b), Line(c), Line(b), Line(a) });
            stages = DialogueStagePlanner.Build(returning);
            Require(stages[2].Left == c && stages[4].Left == a && stages[4].LeftAppearance.Portrait == travel, "Outfit survives exit and reentry");
            Require(DialogueStagePlanner.Build(Sequence(new[] { a }, new[] { Line(a) }))[0].LeftAppearance.Portrait == basic, "No outfit leaks between conversations");
            Require(a.Portrait == basic && !a.FacesLeft, "Character asset default remains unchanged");

            Set(a, "outfits", new[] { outfits[0], new DialogueOutfit { id = "VIAJE", portrait = gala } });
            Require(!a.ValidateOutfits(out _), "Duplicate IDs rejected");
            Set(a, "outfits", new[] { new DialogueOutfit { id = "default", portrait = gala } });
            Require(!a.ValidateOutfits(out _), "Reserved ID rejected");
            Set(a, "outfits", new[] { new DialogueOutfit { id = "missing" } });
            Require(!a.ValidateOutfits(out _), "Missing sprite rejected");
            Set(a, "outfits", outfits);
            Require(!Sequence(new[] { a }, new[] { Line(a, "missing") }).ValidateOutfits(out _), "Unknown line outfit rejected");
            Require(!Sequence(new[] { a }, new[] { Line(a) }, new[] {
                new DialogueOutfitSelection { character = a, outfit = "missing" }
            }).ValidateOutfits(out _), "Unknown initial outfit rejected");
            Require(!Sequence(new[] { a }, new[] { Line(a) }, new[] {
                new DialogueOutfitSelection { character = a, outfit = "viaje" },
                new DialogueOutfitSelection { character = a, outfit = "gala" }
            }).ValidateOutfits(out _), "Duplicate initial selection rejected");
            Require(!Sequence(new[] { a }, new[] { Line(a) }, new[] {
                new DialogueOutfitSelection { character = b, outfit = "viaje" }
            }).ValidateOutfits(out _), "Initial outfit for an absent actor rejected");
            Require(DialogueStagePlanner.Build(Sequence(Array.Empty<DialogueCharacter>(), new[] {
                new DialogueLine { speaker = "Manual", text = "Texto", leftCharacter = travel }
            })).Count == 0, "Legacy manual conversations retain their path");

            ValidateController(a, basic, travel);
            foreach (string guid in AssetDatabase.FindAssets("t:DialogueSequence", new[] { "Assets" }))
            {
                var existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetDatabase.GUIDToAssetPath(guid));
                Require(existing.ValidateOutfits(out string error), error);
                var existingStages = DialogueStagePlanner.Build(existing);
                if (existing.InitialOutfits.Length == 0 && existing.Lines.All(line => line == null || string.IsNullOrEmpty(line.outfit)))
                    foreach (var stage in existingStages)
                    {
                        if (stage.Left != null) Require(stage.LeftAppearance.Portrait == stage.Left.Portrait, "Existing left portrait compatibility");
                        if (stage.Right != null) Require(stage.RightAppearance.Portrait == stage.Right.Portrait, "Existing right portrait compatibility");
                    }
            }
            string result = "PASS: initial outfits, per-line changes, orientation, resets, reentry, replay isolation, validation, existing conversations, controller sprite swap and pagination.";
            File.WriteAllText("Logs/DialogueOutfitValidation.result", result);
            Debug.Log(result);
        }
        catch (Exception error)
        {
            File.WriteAllText("Logs/DialogueOutfitValidation.result", "FAIL: " + error);
            throw;
        }
        finally
        {
            for (int i = temporary.Count - 1; i >= 0; i--) if (temporary[i] != null) UnityEngine.Object.DestroyImmediate(temporary[i]);
            temporary.Clear();
        }
    }

    private static void ValidateController(DialogueCharacter actor, Sprite basic, Sprite outfit)
    {
        var root = Keep(new GameObject("Outfit validation"));
        var controller = root.AddComponent<DialogueController>();
        var imageRoot = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        imageRoot.transform.SetParent(root.transform);
        var image = imageRoot.GetComponent<Image>();
        object focus = Field(controller, "leftFocus");
        Invoke(controller, "SetCharacter", image, basic, actor, false, true, false, focus, true);
        Invoke(controller, "SetCharacter", image, outfit, actor, true, true, false, focus, false);
        Require(image.sprite == outfit && Field(focus, "motion").ToString() == "Focus", "Same actor changes outfit without exit/reentry");
        Invoke(controller, "UpdateFocus", image, focus);
        Require(image.rectTransform.localScale.x < 0, "Outfit facing controls mirroring");
        Invoke(controller, "SetCharacter", image, null, actor, true, true, false, focus, false);
        Require(Field(focus, "motion").ToString() == "Exit", "Manual hiding preserves exit animation");

        var body = new GameObject("Body", typeof(RectTransform), typeof(Text));
        body.transform.SetParent(root.transform);
        var label = body.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 23;
        label.rectTransform.sizeDelta = new Vector2(240, 80);
        Set(controller, "bodyText", label);
        var pageType = typeof(DialogueController).GetNestedType("Page", BindingFlags.NonPublic);
        var page = Activator.CreateInstance(pageType, true);
        Set(page, "text", string.Join(" ", Enumerable.Repeat("Un viaje largo con el mismo vestuario.", 15)));
        Set(page, "leftActor", actor);
        Set(page, "leftCharacter", outfit);
        Set(page, "leftFacesLeft", true);
        Invoke(controller, "AddPages", page);
        var pages = (IList)Field(controller, "pages");
        Require(pages.Count > 1, "Long line pagination");
        foreach (object fragment in pages)
            Require((Sprite)Field(fragment, "leftCharacter") == outfit && (bool)Field(fragment, "leftFacesLeft"), "Every page retains the outfit and facing");
    }

    private static DialogueCharacter Actor(string name, Sprite portrait)
    {
        var actor = Keep(ScriptableObject.CreateInstance<DialogueCharacter>());
        actor.name = name;
        Set(actor, "portrait", portrait);
        return actor;
    }
    private static DialogueLine Line(DialogueCharacter actor, string outfit = null) => new DialogueLine { character = actor, outfit = outfit, text = "Texto de prueba." };
    private static DialogueSequence Sequence(DialogueCharacter[] actors, DialogueLine[] lines, DialogueOutfitSelection[] outfits = null)
    {
        var sequence = Keep(ScriptableObject.CreateInstance<DialogueSequence>());
        Set(sequence, "characters", actors);
        Set(sequence, "lines", lines);
        Set(sequence, "initialOutfits", outfits);
        return sequence;
    }
    private static T Keep<T>(T value) where T : UnityEngine.Object { temporary.Add(value); return value; }
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static object Field(object target, string name) => target.GetType().GetField(name, Fields).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Fields).Invoke(target, args);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
