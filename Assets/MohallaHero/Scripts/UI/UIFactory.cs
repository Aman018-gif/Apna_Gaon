using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>Bright Indian-festival palette: marigold, magenta, teal and saffron on deep indigo "city app" panels.</summary>
    public static class Theme
    {
        public static readonly Color Panel = new Color(0.17f, 0.07f, 0.33f, 0.95f);       // indigo
        public static readonly Color PanelLight = new Color(0.27f, 0.12f, 0.47f, 0.97f);
        public static readonly Color Card = new Color(1f, 0.97f, 0.9f, 1f);                // cream paper
        public static readonly Color CardText = new Color(0.17f, 0.1f, 0.25f, 1f);
        public static readonly Color Row = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color Accent = new Color(1f, 0.76f, 0.05f, 1f);             // marigold #ffc20e
        public static readonly Color AccentDark = new Color(1f, 0.5f, 0.07f, 1f);          // saffron #ff7f11
        public static readonly Color Magenta = new Color(0.9f, 0.08f, 0.48f, 1f);          // #e5157a
        public static readonly Color Teal = new Color(0f, 0.65f, 0.65f, 1f);               // #00a6a6
        public static readonly Color Text = new Color(1f, 0.97f, 0.9f, 1f);
        public static readonly Color TextDim = new Color(0.85f, 0.79f, 0.95f, 1f);
        public static readonly Color Good = new Color(0.36f, 0.9f, 0.36f, 1f);
        public static readonly Color Bad = new Color(1f, 0.33f, 0.33f, 1f);
        public static readonly Color Trust = new Color(0.4f, 0.8f, 1f, 1f);
        public static readonly Color Button = new Color(0.42f, 0.17f, 0.57f, 1f);          // purple
        public static readonly Color ButtonDisabled = new Color(0.35f, 0.3f, 0.42f, 0.85f);
        public static readonly Color Shadow = new Color(0.05f, 0.02f, 0.1f, 0.6f);

        public static Color StatColor(KarmaStat s)
        {
            switch (s)
            {
                case KarmaStat.Civic: return new Color(0f, 0.72f, 0.75f);     // teal
                case KarmaStat.Green: return new Color(0.3f, 0.78f, 0.3f);     // leaf green
                case KarmaStat.Courage: return new Color(1f, 0.5f, 0.07f);     // saffron
                default: return new Color(0.98f, 0.28f, 0.6f);                 // magenta-pink
            }
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>Helpers that build uGUI hierarchies from code (no prefabs).</summary>
    public static class UIFactory
    {
        static Font font;
        public static Font Font => font != null ? font : (font = Compat.DefaultFont());

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float margin = 0f) =>
            rt.Anchor(Vector2.zero, Vector2.one, new Vector2(margin, margin), new Vector2(-margin, -margin));

        /// <summary>Fixed-size rect anchored at a point (pivot follows the anchor).</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>A panel: rounded "app card" corners by default, or a plain rectangle.</summary>
        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded ? SpriteFactory.RoundedRect : SpriteFactory.White;
            if (rounded) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1.2f; }
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 24, TextAnchor align = TextAnchor.UpperLeft, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? Theme.Text;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, int fontSize = 26, Color? color = null)
        {
            var img = Panel(parent, "Button_" + label, color ?? Theme.Button);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.12f, 1.05f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            colors.colorMultiplier = 1.2f;
            btn.colors = colors;
            btn.targetGraphic = img;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;   // keyboard keys are handled by the screens
            btn.navigation = nav;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var t = Label(img.transform, label, fontSize, TextAnchor.MiddleCenter, Theme.Text, FontStyle.Bold);
            t.rectTransform.Stretch(8);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.35f);
            sh.effectDistance = new Vector2(1, -2);
            return btn;
        }

        public static void SetLabel(this Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static Image Icon(Transform parent, Sprite sprite, float size)
        {
            var rt = Rect(parent, "Icon");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = size;
            le.preferredHeight = le.minHeight = size;
            return img;
        }

        /// <summary>A round icon button (touch controls, phone apps).</summary>
        public static Button IconButton(Transform parent, Sprite icon, Color bg, float size, UnityAction onClick, string caption = null)
        {
            var img = Panel(parent, "IconButton", bg, false);
            img.sprite = SpriteFactory.Circle;
            img.type = Image.Type.Simple;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var ic = Icon(img.transform, icon, size * 0.55f);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ic.rectTransform.sizeDelta = new Vector2(size * 0.55f, size * 0.55f);
            ic.rectTransform.anchoredPosition = caption != null ? new Vector2(0, size * 0.08f) : Vector2.zero;
            if (caption != null)
            {
                var t = Label(img.transform, caption, Mathf.RoundToInt(size * 0.16f), TextAnchor.LowerCenter, Theme.Text, FontStyle.Bold);
                t.rectTransform.Stretch(4);
                t.rectTransform.offsetMin = new Vector2(0, size * 0.08f);
            }
            return btn;
        }

        public static LayoutElement Size(this Component c, float width = -1, float height = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) { le.preferredWidth = width; le.minWidth = width; }
            if (height >= 0) { le.preferredHeight = height; le.minHeight = height; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static VerticalLayoutGroup VLayout(this Component c, float spacing = 6, int padding = 0, TextAnchor align = TextAnchor.UpperLeft, bool expandWidth = true)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandWidth;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup HLayout(this Component c, float spacing = 6, int padding = 0, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        /// <summary>A vertical scroll view; returns the content transform (children are laid out vertically).</summary>
        public static RectTransform ScrollList(Transform parent, string name, float spacing = 8)
        {
            var root = Rect(parent, name);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = Rect(root, "Viewport").Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.001f);

            var content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.VLayout(spacing, 4);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>A horizontal bar (background + fill). Returns the fill Image (use fillAmount).</summary>
        public static Image Bar(Transform parent, Color fill, float width, float height)
        {
            var bg = Panel(parent, "Bar", new Color(0, 0, 0, 0.45f), false);
            bg.Size(width, height);
            var f = Panel(bg.transform, "Fill", fill, false);
            f.rectTransform.Stretch(2);
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillAmount = 1f;
            return f;
        }

        public static GameObject Row(Transform parent, float height, Color? bg = null)
        {
            var row = Panel(parent, "Row", bg ?? Theme.Row);
            row.Size(-1, height);
            row.HLayout(12, 10);
            return row.gameObject;
        }

        /// <summary>Row of five star icons (images, so no font glyph is needed).</summary>
        public static RectTransform Stars(Transform parent, int n, float size = 26)
        {
            var row = Rect(parent, "Stars");
            row.HLayout(2);
            row.Size(size * 5 + 8, size);
            for (int i = 0; i < 5; i++) Icon(row, SpriteFactory.StarIcon(i < n), size);
            return row;
        }

        public static void SetStars(RectTransform stars, int n)
        {
            for (int i = 0; i < stars.childCount; i++)
                stars.GetChild(i).GetComponent<Image>().sprite = SpriteFactory.StarIcon(i < n);
        }

        /// <summary>A coloured pill with an icon and a label (karma chips, effect chips).</summary>
        public static Text Chip(Transform parent, Sprite icon, string text, Color bg, int fontSize = 22, float height = 44)
        {
            var p = Panel(parent, "Chip", bg);
            p.HLayout(8, 6, TextAnchor.MiddleLeft);
            p.Size(-1, height);
            if (icon != null) Icon(p.transform, icon, height - 14);
            var t = Label(p.transform, text, fontSize, TextAnchor.MiddleLeft, Theme.Text, FontStyle.Bold);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        public static void AddShadow(Component c, float distance = 4f)
        {
            var sh = c.gameObject.AddComponent<Shadow>();
            sh.effectColor = Theme.Shadow;
            sh.effectDistance = new Vector2(distance, -distance);
        }
    }
}
