using Deeper.Core;
using Deeper.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deeper.EditorTools
{
    /// <summary>
    /// The primitives every HUD layout tool is built from — the canvas, the anchoring, the widget
    /// constructors and the serialized-field wiring.
    ///
    /// Extracted from <see cref="BuildRunHUD"/> when the upgrade offer needed a second tool. Two
    /// tools cannot share a canvas contract by each keeping their own copy of it: the whole-number
    /// scale factor, the half-size authoring rule and the "wire it in the tool, never by dragging"
    /// rule are one contract, and a second copy is a second place for them to drift.
    ///
    /// <b>Everything here is measured in AUTHORED units, which are half their on-screen size.</b>
    /// <c>PixelPerfectHUDScale</c> scales the canvas by a whole number from a 540 reference, so the
    /// normal factor at 1080p is 2 and a 1px detail is a 2px detail on screen.
    /// </summary>
    internal static class HUDLayout
    {
        public const string CanvasName = "HUDCanvas";
        public const string ArtFolder = "Assets/_Main/Art/UI/";
        public const string InputAssetPath = "Assets/_Main/Input/InputSystem_Actions.inputactions";

        /// <summary>Matches <c>PixelPerfectHUDScale.referenceHeight</c>. Both must move together.</summary>
        public const int ReferenceHeight = 540;

        /// <summary>The font's native size. Text at this size lands 1:1 on the canvas's pixel grid.</summary>
        public const int BodyText = 7;

        /// <summary>Exactly 2x native — the only other size that stays on the grid.</summary>
        public const int TitleText = 14;

        /// <summary>Kept for the scaler's inspector; the ConstantPixelSize mode ignores it.</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // ---------------------------------------------------------------- canvas

        public static Canvas FindOrCreateCanvas()
        {
            GameObject go = GameObject.Find(CanvasName);
            if (go == null)
            {
                go = new GameObject(CanvasName);
                go.AddComponent<Canvas>();
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // The offer screen is clicked, so the canvas needs a raycaster even though every widget
            // the run HUD puts on it sets raycastTarget false.
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();

            var scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
            scaler.referenceResolution = ReferenceResolution;

            // NOT ScaleWithScreenSize. That mode produces a fractional factor at any window size
            // other than the reference — 0.45 in a 906x463 editor Game view — and a fractional factor
            // resamples point-filtered art off its grid until the whole HUD reads as flat untextured
            // bars. PixelPerfectHUDScale owns the mode and the factor from here.
            if (go.GetComponent<PixelPerfectHUDScale>() == null) go.AddComponent<PixelPerfectHUDScale>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Mathf.Max(1, Screen.height / ReferenceHeight);

            return canvas;
        }

        /// <summary>
        /// The scene's single <c>RunPause</c>, created on the HUD canvas if it has none.
        ///
        /// One per scene, deliberately. It refcounts holds on <c>Time.timeScale</c> and the Player
        /// action map, and two of them would each keep their own count — which is the double-toggle
        /// bug the refcount exists to remove, reintroduced one level up.
        /// </summary>
        public static RunPause EnsureRunPause(Canvas canvas)
        {
            RunPause pause = Object.FindFirstObjectByType<RunPause>(FindObjectsInactive.Include);

            if (pause == null && canvas != null) pause = canvas.gameObject.AddComponent<RunPause>();
            if (pause == null) return null;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (actions != null) Wire(pause, "inputActions", actions);

            Wire(pause, "hitStop", PlayerPart<Deeper.Combat.HitStop>());

            return pause;
        }

        // ---------------------------------------------------------------- assets

        /// <summary>
        /// The player rig's copy of a component, or null when the open scene has no tagged player.
        ///
        /// Every HUD class already falls back to this same tag lookup in its own <c>Awake</c>, so
        /// this is not what makes the HUD work — it is what makes each connection <b>visible in the
        /// Inspector</b> rather than discovered at runtime, which is the house rule.
        /// </summary>
        public static T PlayerPart<T>() where T : Component
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.GetComponentInChildren<T>(true) : null;
        }

        public static Sprite Load(string file)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file + ".png");
            if (sprite == null)
            {
                Debug.LogWarning("Missing HUD sprite " + ArtFolder + file + ".png — the element " +
                                 "will build without its frame art.");
            }

            return sprite;
        }

        public static Vector2 SizeOf(Sprite sprite, Vector2 fallback)
        {
            return sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : fallback;
        }

        /// <summary>
        /// The generated pixel font, or the built-in face if it has not been generated yet.
        ///
        /// Not cached: regenerating the font and rebuilding the HUD in the same editor session is
        /// the normal loop while tuning, and a cached reference there hands out the old asset.
        /// </summary>
        public static Font PixelFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "HUD_Font.fontsettings");
            if (font != null) return font;

            Debug.LogWarning("No HUD_Font.fontsettings in " + ArtFolder + " — run " +
                             "Deeper/Generate HUD Font first. Falling back to the built-in face, " +
                             "which is anti-aliased and will not match the rest of the HUD.");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ---------------------------------------------------------------- widgets

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// <paramref name="clickable"/> defaults to false because the run HUD is a readout and must
        /// never eat clicks. The offer screen is the exception: its cards and its scrim are the only
        /// things in the project that are meant to be pressed.
        /// </summary>
        public static Image AddImage(RectTransform rect, Sprite sprite, Color colour, bool clickable = false)
        {
            Image image = rect.gameObject.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();

            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = clickable;
            return image;
        }

        public static Text AddText(RectTransform parent, string content, int size, TextAnchor align,
                                   bool wrap = false)
        {
            // Always its own object. Text and Image both derive from Graphic, and Unity does not
            // support two Graphics on one GameObject — they fight over the same canvas renderer and
            // one of them silently does not draw.
            RectTransform rect = NewRect("Label", parent);
            Stretch(rect);

            return Decorate(rect, content, size, align, wrap);
        }

        /// <summary>
        /// The same label, on a rect the caller has already made and positioned. A card's name and
        /// description need their own boxes inside the card, which <see cref="AddText"/>'s stretched
        /// child cannot give them.
        /// </summary>
        public static Text AddTextIn(RectTransform rect, string content, int size, TextAnchor align,
                                     bool wrap = false)
        {
            return Decorate(rect, content, size, align, wrap);
        }

        private static Text Decorate(RectTransform rect, string content, int size, TextAnchor align, bool wrap)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = PixelFont();

            // A hard drop shadow, offset by one authored pixel. Without it the labels sit directly on
            // the world and a pale digit over a pale floor tile is unreadable; a soft shadow would be
            // the one anti-aliased thing left in the HUD.
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;

            return text;
        }

        // ---------------------------------------------------------------- wiring

        /// <summary>Fills a serialized array field with references, in order.</summary>
        public static void WireArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null || !prop.isArray)
            {
                Debug.LogWarning("No serialized array '" + field + "' on " + target.GetType().Name);
                return;
            }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sets a private serialized field by name, so the built HUD is wired exactly the
        /// way a human dragging references would leave it.</summary>
        public static void Wire(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning("No serialized field '" + field + "' on " + target.GetType().Name);
                return;
            }

            if (value is Color) prop.colorValue = (Color)value;
            else if (value is int) prop.intValue = (int)value;
            else if (value is float) prop.floatValue = (float)value;
            else if (value is bool) prop.boolValue = (bool)value;
            else if (value is string) prop.stringValue = (string)value;
            else prop.objectReferenceValue = (Object)value;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- anchoring

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary><paramref name="by"/> is (left, bottom, right, top) in pixels.</summary>
        public static void Inset(RectTransform rect, Vector4 by)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(by.x, by.y);
            rect.offsetMax = new Vector2(-by.z, -by.w);
        }

        public static void Centre(RectTransform rect, Vector2 size)
        {
            Centre(rect, size, Vector2.zero);
        }

        public static void Centre(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        public static void AnchorTopLeft(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        public static void AnchorTopRight(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        public static void AnchorTopCentre(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        public static void AnchorBottomCentre(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        /// <summary>Top-centre of the card row, measured down from the middle of the screen.</summary>
        public static void AnchorCentreOffset(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }
    }
}
