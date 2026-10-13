using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;

namespace RetroDiner.UI
{
    /// <summary>
    /// Helpers to build uGUI hierarchies from code so no scene/prefab YAML has to be hand-authored.
    /// Uses the built-in legacy font so text renders without importing TMP essentials; swapping to
    /// TextMeshPro later only touches this file.
    /// </summary>
    public static class UIBuilder
    {
        public static readonly Color Cream = new Color(1f, 0.957f, 0.863f);
        public static readonly Color Red = new Color(0.843f, 0.149f, 0.239f);
        public static readonly Color Mint = new Color(0.498f, 0.820f, 0.761f);
        public static readonly Color Gold = new Color(0.965f, 0.725f, 0.231f);
        public static readonly Color Chrome = new Color(0.80f, 0.82f, 0.86f);
        public static readonly Color Dark = new Color(0.13f, 0.10f, 0.10f);
        public static readonly Color Sky = new Color(0.36f, 0.68f, 0.93f);
        public static readonly Color Purple = new Color(0.55f, 0.36f, 0.82f);
        public static readonly Color Orange = new Color(0.98f, 0.58f, 0.18f);

        public const float RefWidth = 1080f;
        public const float RefHeight = 1920f;

        static Font font, bodyFont, builtin;
        static bool fontsProbed;

        static void ProbeFonts()
        {
            if (fontsProbed) return;
            fontsProbed = true;
            // Licensed rounded display + readable body face, dropped into Assets/Resources/RetroDiner/Fonts by the artist.
            font = Resources.Load<Font>("RetroDiner/Fonts/Fredoka-Bold");
            if (font == null) font = Resources.Load<Font>("RetroDiner/Fonts/Fredoka-SemiBold");
            bodyFont = Resources.Load<Font>("RetroDiner/Fonts/Nunito-SemiBold");
            if (bodyFont == null) bodyFont = Resources.Load<Font>("RetroDiner/Fonts/Nunito-Bold");
            try { builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (builtin == null) { try { builtin = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (font == null) font = builtin;
            if (bodyFont == null) bodyFont = builtin;
        }

        /// <summary>Display face (titles, buttons, counts).</summary>
        public static Font Font { get { ProbeFonts(); return font; } }
        /// <summary>Supporting face (body copy, captions).</summary>
        public static Font BodyFont { get { ProbeFonts(); return bodyFont; } }
        public static bool HasCustomFonts { get { ProbeFonts(); return font != builtin; } }

        public static Canvas CreateCanvas(string name, int sortingOrder, RenderMode mode = RenderMode.ScreenSpaceOverlay, Camera cam = null, int layer = 5)
        {
            var go = new GameObject(name);
            go.layer = layer;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = mode;
            canvas.sortingOrder = sortingOrder;
            if (cam != null) { canvas.worldCamera = cam; canvas.planeDistance = 10f; }
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchor at a normalized point of the parent with a fixed size.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = offset;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = offset;
        }

        public static Image Panel(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null) img.preserveAspect = false;
            return img;
        }

        /// <summary>Rounded pill/card background (9-sliced procedural sprite).</summary>
        public static Image Pill(Transform parent, string name, Color color, int radius = 24, bool raycast = false)
        {
            var img = Panel(parent, name, color, ProcSprites.Rounded(radius), raycast);
            img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>Soft drop shadow behind a rect (a second blurred-looking pill offset downward).</summary>
        public static Image SoftShadow(RectTransform target, float offset = 10f, float alpha = 0.35f, int radius = 28)
        {
            var sh = Pill(target.parent, target.name + "_Shadow", new Color(0f, 0f, 0f, alpha), radius);
            sh.rectTransform.anchorMin = target.anchorMin; sh.rectTransform.anchorMax = target.anchorMax; sh.rectTransform.pivot = target.pivot;
            sh.rectTransform.sizeDelta = target.sizeDelta + new Vector2(8f, 8f);
            sh.rectTransform.anchoredPosition = target.anchoredPosition + new Vector2(0f, -offset);
            sh.transform.SetSiblingIndex(target.GetSiblingIndex());
            return sh;
        }

        /// <summary>Gently pulses a RectTransform's scale forever (call-to-action buttons).</summary>
        public static void Pulse(RectTransform rt, float amount = 0.04f, float period = 1.4f)
        {
            if (rt == null) return;
            var p = rt.gameObject.AddComponent<PulseScale>();
            p.amount = amount; p.period = period;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Color fallbackColor, Vector2 size)
        {
            var img = Panel(parent, name, sprite != null ? Color.white : fallbackColor, sprite, false);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = size;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = style == FontStyle.Normal ? BodyFont : Font;
            if (HasCustomFonts) style = FontStyle.Normal;   // weight comes from the face itself
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        public static Outline Outline(Graphic g, Color color, float size = 2f)
        {
            // Text gets a restrained outline (the heavy black ring was the main "disconnected label" problem).
            if (g is Text) { size = Mathf.Min(size, 1.6f); color.a *= 0.7f; }
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(size, -size);
            return o;
        }

        /// <summary>Sliced image from a 9-slice-authored sprite; falls back to a procedural pill.</summary>
        public static Image Sliced(Transform parent, string name, Sprite nineSlice, Color fallbackColor, int fallbackRadius = 24, bool raycast = false)
        {
            if (nineSlice == null) return Pill(parent, name, fallbackColor, fallbackRadius, raycast);
            var img = Panel(parent, name, Color.white, nineSlice, raycast);
            img.type = nineSlice.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            return img;
        }

        public static Shadow Shadow(Graphic g, float size = 4f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.45f);
            s.effectDistance = new Vector2(size * 0.5f, -size);
            return s;
        }

        public static Button Button(Transform parent, string name, string label, Action onClick, Color color, Sprite sprite, Vector2 size, int fontSize = 40, Color? textColor = null)
        {
            var img = Panel(parent, name, sprite != null ? Color.white : color, sprite, true);
            img.rectTransform.sizeDelta = size;
            img.preserveAspect = sprite != null;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            btn.colors = colors;
            if (sprite == null)
            {
                img.sprite = ProcSprites.Rounded(Mathf.Clamp((int)(size.y * 0.28f), 10, 60));
                img.type = Image.Type.Sliced;
                Shadow(img, 5f);
                // Glossy top highlight for code-drawn buttons.
                var rim = Panel(img.transform, "Rim", new Color(1f, 1f, 1f, 0.18f), ProcSprites.Rounded(Mathf.Clamp((int)(size.y * 0.28f), 10, 60)), false);
                rim.type = Image.Type.Sliced;
                Stretch(rim.rectTransform, 4, 4, 4, size.y * 0.5f);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Label(img.transform, "Label", label, fontSize, textColor ?? Color.white);
                Stretch(t.rectTransform, 12, 6, 12, 6);
                Outline(t, new Color(0f, 0f, 0f, 0.5f), 2f);
            }
            if (onClick != null) btn.onClick.AddListener(() =>
            {
                if (GameServices.Audio != null) GameServices.Audio.Play("ui", 0.6f);
                onClick();
            });
            // Press squash.
            var squash = btn.gameObject.AddComponent<PressSquash>();
            squash.target = img.rectTransform;
            return btn;
        }

        public static RectTransform CanvasRect(Component c) => c.GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();

        /// <summary>Screen pixel point to local point in a stretched layer of an overlay canvas.</summary>
        public static Vector2 ScreenToLocal(RectTransform layer, Vector2 screen, Camera cam = null)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, cam, out local);
            return local;
        }

        public static Vector2 LocalToScreen(RectTransform rt, Camera cam = null)
        {
            return RectTransformUtility.WorldToScreenPoint(cam, rt.position);
        }

        public static void Punch(RectTransform rt, float amount = 0.18f, float duration = 0.25f)
        {
            if (rt == null) return;
            Vector3 baseScale = Vector3.one;
            rt.localScale = baseScale * (1f + amount);
            Tween.To(duration, t => { if (rt != null) rt.localScale = Vector3.LerpUnclamped(baseScale * (1f + amount), baseScale, t); }, Ease.OutBack, null, true);
        }
    }

    /// <summary>Idle breathing scale for call-to-action elements.</summary>
    public class PulseScale : MonoBehaviour
    {
        public float amount = 0.04f, period = 1.4f;
        float t;
        void Update()
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(t / period * Mathf.PI * 2f) * amount;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Scales a button down while pressed; tiny tactile touch.</summary>
    public class PressSquash : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public RectTransform target;
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { if (target != null) target.localScale = Vector3.one * 0.93f; }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) { if (target != null) Tween.Scale(target, Vector3.one, 0.2f, Ease.OutBack, null, true); }
    }
}
