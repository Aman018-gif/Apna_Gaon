using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MohallaHero
{
    public enum NotifyStyle { Normal, Big }

    public abstract class UIScreen : MonoBehaviour
    {
        protected UIManager ui;
        protected GameManager gm => GameManager.I;
        public virtual bool CloseOnEscape => true;
        public void Init(UIManager ui) { this.ui = ui; Build(); }
        protected abstract void Build();
        public virtual void OnOpen() { }
        public virtual void OnClose() { }

        /// <summary>Standard "city app" window: dimmed backdrop, rounded card, coloured header bar. Returns the body.</summary>
        protected RectTransform Window(string title, Vector2 size, bool closeButton = true, Color? header = null)
        {
            var dim = UIFactory.Panel(transform, "Dim", new Color(0.05f, 0.02f, 0.1f, 0.55f), false);
            dim.rectTransform.Stretch();
            var win = UIFactory.Panel(transform, "Window", Theme.Panel);
            win.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            win.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            UIFactory.AddShadow(win, 8);

            var bar = UIFactory.Panel(win.transform, "Header", header ?? Theme.Magenta);
            bar.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -76), Vector2.zero);
            var titleText = UIFactory.Label(bar.transform, title, 34, TextAnchor.MiddleLeft, Theme.Text, FontStyle.Bold);
            titleText.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(28, 0), new Vector2(-90, 0));
            titleText.name = "Title";
            if (closeButton)
            {
                var close = UIFactory.Button(bar.transform, "X", () => ui.Close(this), 28, new Color(0, 0, 0, 0.25f));
                close.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(60, 56));
            }
            var body = UIFactory.Rect(win.transform, "Body");
            body.Anchor(Vector2.zero, Vector2.one, new Vector2(24, 24), new Vector2(-24, -94));
            return body;
        }

        protected void SetTitle(string title)
        {
            var t = transform.Find("Window/Header/Title");
            if (t != null) t.GetComponent<Text>().text = title;
        }
    }

    /// <summary>Owns the canvas, every screen, modal state, notifications, subtitles, fades and UI hotkeys.</summary>
    public class UIManager : MonoBehaviour
    {
        public RectTransform Root { get; private set; }
        public MainMenuScreen Menu { get; private set; }
        public HudScreen Hud { get; private set; }
        public TouchControls Touch { get; private set; }
        public DialogueScreen Dialogue { get; private set; }
        public WhyItMattersScreen Why { get; private set; }
        public PhoneScreen Phone { get; private set; }
        public MapScreen Map { get; private set; }
        public PauseScreen Pause { get; private set; }
        public WasteSortingScreen Waste { get; private set; }
        public FakeNewsScreen FakeNews { get; private set; }
        public SloganScreen Slogan { get; private set; }
        public VotingScreen Voting { get; private set; }
        public EndingScreen Ending { get; private set; }

        UIScreen modal;
        int modalOpenedFrame = -1;
        int lastClosedFrame = -1;
        RectTransform toastRoot;
        Text bigBanner;
        float bigBannerTime;
        Image fader;
        Text faderText;
        bool fading;
        GameObject subtitleBox;
        Text subtitleText;
        float subtitleTime;
        readonly List<(GameObject go, float time)> toasts = new List<(GameObject, float)>();

        public bool IsModalOpen => modal != null || fading;
        public bool IsFading => fading;
        public bool JustOpened => Time.frameCount == modalOpenedFrame;
        public bool ClosedThisFrame => Time.frameCount == lastClosedFrame;
        public UIScreen CurrentModal => modal;
        public bool PointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        public static UIManager Create(Transform parent)
        {
            EnsureEventSystem();
            var go = new GameObject("UI");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var ui = go.AddComponent<UIManager>();
            ui.Build();
            return ui;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyEventSystem() != null) return;
            var es = new GameObject("EventSystem");
            DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        static EventSystem FindAnyEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            return FindAnyObjectByType<EventSystem>();
#else
            return FindObjectOfType<EventSystem>();
#endif
        }

        void Build()
        {
            Root = (RectTransform)transform;
            Hud = AddScreen<HudScreen>("HUD");
            Touch = TouchControls.Create(Root);
            Dialogue = AddScreen<DialogueScreen>("Dialogue");
            Why = AddScreen<WhyItMattersScreen>("WhyItMatters");
            Phone = AddScreen<PhoneScreen>("Phone");
            Map = AddScreen<MapScreen>("Map");
            Pause = AddScreen<PauseScreen>("Pause");
            Waste = AddScreen<WasteSortingScreen>("WasteSorting");
            FakeNews = AddScreen<FakeNewsScreen>("FakeNews");
            Slogan = AddScreen<SloganScreen>("Slogan");
            Voting = AddScreen<VotingScreen>("Voting");
            Ending = AddScreen<EndingScreen>("Ending");
            Menu = AddScreen<MainMenuScreen>("MainMenu");

            // subtitles for ambient speech (loudspeaker, crowd, Pintu's hints)
            var sb = UIFactory.Panel(Root, "Subtitle", new Color(0.05f, 0.02f, 0.1f, 0.78f));
            sb.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 236), new Vector2(1100, 70));
            subtitleText = UIFactory.Label(sb.transform, "", 26, TextAnchor.MiddleCenter);
            subtitleText.rectTransform.Stretch(10);
            subtitleBox = sb.gameObject;
            subtitleBox.SetActive(false);

            toastRoot = UIFactory.Rect(Root, "Toasts");
            toastRoot.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-460, -620), new Vector2(460, -210));
            toastRoot.VLayout(8, 0, TextAnchor.UpperCenter);

            bigBanner = UIFactory.Label(Root, "", 58, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            bigBanner.rectTransform.Anchor(new Vector2(0, 0.58f), new Vector2(1, 0.76f), Vector2.zero, Vector2.zero);
            var ol = bigBanner.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.35f, 0.05f, 0.25f, 0.95f);
            ol.effectDistance = new Vector2(3, -3);

            fader = UIFactory.Panel(Root, "Fader", new Color(0, 0, 0, 0), false);
            fader.rectTransform.Stretch();
            fader.raycastTarget = false;
            faderText = UIFactory.Label(fader.transform, "", 46, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            faderText.rectTransform.Stretch();
        }

        T AddScreen<T>(string name) where T : UIScreen
        {
            var rt = UIFactory.Rect(Root, name).Stretch();
            var s = rt.gameObject.AddComponent<T>();
            s.Init(this);
            rt.gameObject.SetActive(false);
            return s;
        }

        // ------------------------------------------------------------------ screens

        public void Open(UIScreen s)
        {
            if (modal != null && modal != s) Close(modal);
            modal = s;
            s.gameObject.SetActive(true);
            s.transform.SetAsLastSibling();
            toastRoot.SetAsLastSibling();
            bigBanner.transform.SetAsLastSibling();
            fader.transform.SetAsLastSibling();
            s.OnOpen();
            modalOpenedFrame = Time.frameCount;
        }

        public void Close(UIScreen s)
        {
            if (s == null || !s.gameObject.activeSelf) { if (modal == s) modal = null; return; }
            s.gameObject.SetActive(false);
            if (modal == s) modal = null;
            lastClosedFrame = Time.frameCount;
            s.OnClose();
        }

        public void CloseModal() => Close(modal);

        public void ShowMainMenu()
        {
            CloseModal();
            Hud.gameObject.SetActive(false);
            Touch.gameObject.SetActive(false);
            subtitleBox.SetActive(false);
            ClearToasts();
            Menu.gameObject.SetActive(true);
            Menu.transform.SetAsLastSibling();
            fader.transform.SetAsLastSibling();
            Menu.OnOpen();
        }

        public void ShowGameUI()
        {
            Menu.gameObject.SetActive(false);
            Hud.gameObject.SetActive(true);
            Hud.transform.SetAsFirstSibling();
            Touch.transform.SetSiblingIndex(1);
        }

        public void OpenMiniGame(string name)
        {
            switch (name)
            {
                case "waste": Open(Waste); break;
                case "fakenews": Open(FakeNews); break;
                case "slogan": Open(Slogan); break;
                default: Debug.LogWarning("Mohalla Hero: unknown mini-game " + name); break;
            }
        }

        public void ShowHint()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.InWorld) return;
            Subtitle("Pintu", gm.Missions.Hint(), 7f);
            if (!gm.Voice.Busy) gm.Voice.Say(VoiceSystem.ForNpc(NpcDatabase.Get(NpcId.Pintu)), gm.Missions.Hint());
        }

        // ------------------------------------------------------------------ notifications

        public void Notify(string message, NotifyStyle style = NotifyStyle.Normal)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (style == NotifyStyle.Big)
            {
                bigBanner.text = message;
                bigBannerTime = 4f;
                bigBanner.transform.SetAsLastSibling();
                fader.transform.SetAsLastSibling();
                return;
            }
            if (toasts.Count > 0 && toasts[toasts.Count - 1].go.GetComponentInChildren<Text>().text == message)
            {
                toasts[toasts.Count - 1] = (toasts[toasts.Count - 1].go, 4f);
                return;
            }
            var bg = UIFactory.Panel(toastRoot, "Toast", new Color(0.17f, 0.07f, 0.33f, 0.92f));
            bg.HLayout(0, 14, TextAnchor.MiddleCenter);
            var t = UIFactory.Label(bg.transform, message, 26, TextAnchor.MiddleCenter);
            t.Size(860);
            toasts.Add((bg.gameObject, 4f));
            while (toasts.Count > 4)
            {
                Destroy(toasts[0].go);
                toasts.RemoveAt(0);
            }
        }

        /// <summary>A subtitle line for speech that isn't part of a conversation.</summary>
        public void Subtitle(string speaker, string text, float seconds = 5f)
        {
            var gm = GameManager.I;
            if (gm != null && !gm.Settings.Subtitles && speaker != "Pintu") return;
            subtitleText.text = string.IsNullOrEmpty(speaker) ? text : $"<color=#ffc20e><b>{speaker}:</b></color> {text}";
            subtitleTime = seconds;
            subtitleBox.SetActive(true);
        }

        void ClearToasts()
        {
            foreach (var t in toasts) if (t.go != null) Destroy(t.go);
            toasts.Clear();
            bigBannerTime = 0f;
            bigBanner.text = "";
        }

        // ------------------------------------------------------------------ fade

        public void Fade(string caption, Action atBlack, Action done = null)
        {
            if (fading) return;
            StartCoroutine(FadeRoutine(caption, atBlack, done));
        }

        IEnumerator FadeRoutine(string caption, Action atBlack, Action done)
        {
            fading = true;
            fader.raycastTarget = true;
            fader.transform.SetAsLastSibling();
            faderText.text = "";
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
            {
                fader.color = new Color(0.08f, 0.03f, 0.15f, t);
                yield return null;
            }
            fader.color = new Color(0.08f, 0.03f, 0.15f, 1f);
            faderText.text = caption;
            atBlack?.Invoke();
            yield return new WaitForSecondsRealtime(string.IsNullOrEmpty(caption) ? 0.3f : 1.4f);
            faderText.text = "";
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.45f)
            {
                fader.color = new Color(0.08f, 0.03f, 0.15f, t);
                yield return null;
            }
            fader.color = new Color(0, 0, 0, 0);
            fader.raycastTarget = false;
            fading = false;
            done?.Invoke();
        }

        // ------------------------------------------------------------------ update / hotkeys

        void Update()
        {
            // Notifications wait while a window is open, so they never cover it.
            bool hold = modal != null;
            toastRoot.gameObject.SetActive(!hold);
            bigBanner.enabled = !hold;
            for (int i = toasts.Count - 1; i >= 0 && !hold; i--)
            {
                var (go, time) = toasts[i];
                time -= Time.unscaledDeltaTime;
                if (time <= 0f) { Destroy(go); toasts.RemoveAt(i); continue; }
                toasts[i] = (go, time);
                var img = go.GetComponent<Image>();
                if (time < 0.5f && img != null) img.color = new Color(0.17f, 0.07f, 0.33f, 0.92f * time / 0.5f);
            }
            if (bigBannerTime > 0f && !hold)
            {
                bigBannerTime -= Time.unscaledDeltaTime;
                bigBanner.color = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, Mathf.Clamp01(bigBannerTime));
                if (bigBannerTime <= 0f) bigBanner.text = "";
            }
            if (subtitleTime > 0f)
            {
                subtitleTime -= Time.unscaledDeltaTime;
                if (subtitleTime <= 0f || modal != null) { subtitleTime = 0f; subtitleBox.SetActive(false); }
            }

            var gm = GameManager.I;
            if (gm == null || !gm.InWorld || fading) { Touch.SetVisible(false); return; }
            Touch.SetVisible(modal == null && gm.Settings.TouchControlsVisible);

            if (modal != null)
            {
                if (JustOpened) return;
                if (GameInput.Down(GameKey.Pause) && modal.CloseOnEscape)
                {
                    if (modal == Dialogue) { if (gm.Dialogue.CanCancel) gm.Dialogue.Cancel(); }
                    else CloseModal();
                    return;
                }
                if (GameInput.Down(GameKey.Phone) && modal == Phone) CloseModal();
                else if (GameInput.Down(GameKey.Map) && modal == Map) CloseModal();
                return;
            }

            if (GameInput.Down(GameKey.Phone)) Open(Phone);
            else if (GameInput.Down(GameKey.Map)) Open(Map);
            else if (GameInput.Down(GameKey.Pause)) Open(Pause);
            else if (GameInput.Down(GameKey.Hint)) ShowHint();
            else if (GameInput.Down(GameKey.QuickSave)) { gm.SaveGame(); Notify("Game saved."); }
        }
    }
}
