using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// Mobile controls: a floating joystick (bottom-left, push to the edge to run) and large thumb buttons
    /// (bottom-right): Action, Phone, Map, Hint, plus Pause in the corner. Shown automatically on touch screens
    /// (Settings → Touch controls: Auto / On / Off). Buttons feed <see cref="GameInput"/> as virtual key presses.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        bool visible = true;

        const float S = 1.3f;   // thumb buttons are 30% bigger than the desktop-sized design

        public static TouchControls Create(Transform parent)
        {
            var root = UIFactory.Rect(parent, "TouchControls").Stretch();
            var tc = root.gameObject.AddComponent<TouchControls>();
            tc.Build(root);
            root.gameObject.SetActive(false);
            return tc;
        }

        void Build(RectTransform root)
        {
            // Joystick area: the whole lower-left quarter, so the thumb can land anywhere
            var area = UIFactory.Panel(root, "StickArea", new Color(1, 1, 1, 0.001f), false);
            area.rectTransform.Anchor(Vector2.zero, new Vector2(0.4f, 0.55f), Vector2.zero, Vector2.zero);
            var stick = area.gameObject.AddComponent<VirtualStick>();
            var bgImg = UIFactory.Panel(area.transform, "Base", new Color(1, 1, 1, 0.16f), false);
            bgImg.sprite = SpriteFactory.Circle;
            bgImg.rectTransform.anchorMin = bgImg.rectTransform.anchorMax = Vector2.zero;
            bgImg.rectTransform.sizeDelta = new Vector2(260, 260) * S;
            bgImg.raycastTarget = false;
            var knob = UIFactory.Panel(bgImg.transform, "Knob", new Color(1f, 0.76f, 0.05f, 0.75f), false);
            knob.sprite = SpriteFactory.Circle;
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            knob.rectTransform.sizeDelta = new Vector2(110, 110) * S;
            knob.raycastTarget = false;
            stick.Init(bgImg.rectTransform, knob.rectTransform, new Vector2(230, 230) * S);

            // Thumb buttons
            var action = UIFactory.IconButton(root, SpriteFactory.AppIcon("talk"), new Color(0f, 0.65f, 0.65f, 0.9f), 190 * S, () => GameInput.Press(GameKey.Interact), "ACTION");
            action.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-60, 60) * S, new Vector2(190, 190) * S);
            var phone = UIFactory.IconButton(root, SpriteFactory.AppIcon("phone"), new Color(0.9f, 0.08f, 0.48f, 0.9f), 120 * S, () => GameInput.Press(GameKey.Phone), "PHONE");
            phone.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-280, 50) * S, new Vector2(120, 120) * S);
            var map = UIFactory.IconButton(root, SpriteFactory.AppIcon("map"), new Color(0.42f, 0.17f, 0.57f, 0.9f), 120 * S, () => GameInput.Press(GameKey.Map), "MAP");
            map.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-90, 270) * S, new Vector2(120, 120) * S);
            var hint = UIFactory.IconButton(root, SpriteFactory.AppIcon("hint"), new Color(1f, 0.5f, 0.07f, 0.9f), 110 * S, () => GameInput.Press(GameKey.Hint), "PINTU");
            hint.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-240, 205) * S, new Vector2(110, 110) * S);
            var pause = UIFactory.IconButton(root, SpriteFactory.AppIcon("settings"), new Color(0.17f, 0.07f, 0.33f, 0.9f), 90 * S, () => GameInput.Press(GameKey.Pause));
            pause.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1), new Vector2(0, -104), new Vector2(90, 90) * S);
        }

        public void SetVisible(bool on)
        {
            if (visible == on) return;
            visible = on;
            gameObject.SetActive(on);
            if (!on) GameInput.VirtualMove = Vector2.zero;
        }
    }

    /// <summary>A floating virtual joystick: appears where the thumb touches, outputs to GameInput.VirtualMove.</summary>
    public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform baseRt, knob, area;
        Vector2 rest;
        const float Radius = 100f;

        public void Init(RectTransform baseRt, RectTransform knob, Vector2 restPosition)
        {
            this.baseRt = baseRt;
            this.knob = knob;
            area = (RectTransform)transform;
            rest = restPosition;
            baseRt.anchoredPosition = rest;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out var local))
                baseRt.anchoredPosition = local - area.rect.min;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRt, e.position, e.pressEventCamera, out var local)) return;
            var v = Vector2.ClampMagnitude(local, Radius);
            knob.anchoredPosition = v;
            GameInput.VirtualMove = v / Radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            knob.anchoredPosition = Vector2.zero;
            baseRt.anchoredPosition = rest;
            GameInput.VirtualMove = Vector2.zero;
        }

        void OnDisable()
        {
            GameInput.VirtualMove = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }
    }
}
