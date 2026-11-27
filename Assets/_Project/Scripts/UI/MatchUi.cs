using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RockAndHardPlace
{
    internal static class MatchUi
    {
        public static readonly Color Panel = new Color32(26, 38, 45, 247);
        public static readonly Color Edge = new Color32(108, 126, 126, 255);
        public static readonly Color TextColor = new Color32(249, 241, 219, 255);
        public static readonly Color Muted = new Color32(195, 210, 209, 255);
        public static readonly Color Gold = new Color32(247, 204, 133, 255);
        public static readonly Color Blue = new Color32(141, 200, 251, 255);
        public static readonly Color Red = new Color32(255, 162, 146, 255);

        public static Color Team(Turret turret) => turret.DisplayName == "Red" ? Red : Blue;

        public static RectTransform Child(RectTransform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform At(RectTransform parent, string name, float x, float y, float width, float height)
        {
            var rect = Child(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static Image Fill(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static TMP_Text Text(RectTransform rect, TMP_FontAsset font, string value, float size,
            Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft, bool bold = false)
        {
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.raycastTarget = false;
            return text;
        }

        public static Button Control(RectTransform rect, Action action, bool primary = false)
        {
            var background = Fill(rect, Color.white, true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = primary ? Gold : new Color32(52, 70, 79, 255);
            colors.highlightedColor = primary ? new Color32(255, 225, 168, 255) : new Color32(77, 101, 112, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = primary ? new Color32(212, 167, 100, 255) : new Color32(40, 55, 64, 255);
            colors.disabledColor = new Color32(43, 54, 60, 255);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => action());
            return button;
        }

        public static Button LabeledButton(RectTransform rect, TMP_FontAsset font, string label, Action action,
            bool primary = false, float size = 26)
        {
            var button = Control(rect, action, primary);
            var labelRect = Child(rect, "Label");
            Stretch(labelRect, 6);
            Text(labelRect, font, label, size, primary ? Panel : TextColor, TextAlignmentOptions.Center, true);
            return button;
        }
    }
}
