using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// Full map of Shanti Nagar: labelled landmarks and homes, residents as dots (orange = has something for you),
    /// the player and the next objective.
    /// </summary>
    public class MapScreen : UIScreen
    {
        RawImage mapImage;
        RectTransform frame, playerMarker, objectiveMarker;
        Texture2D tex;
        Text legend;
        readonly Dictionary<NpcId, Image> npcDots = new Dictionary<NpcId, Image>();
        const float Scale = 13f;

        protected override void Build()
        {
            var body = Window("Shanti Nagar ka Naksha (Map)", new Vector2(WorldLayout.Width * Scale + 60, WorldLayout.Height * Scale + 190), true, new Color(0.2f, 0.45f, 0.85f));
            frame = UIFactory.Rect(body, "Frame");
            frame.Place(new Vector2(0.5f, 1), Vector2.zero, new Vector2(WorldLayout.Width * Scale, WorldLayout.Height * Scale));
            mapImage = frame.gameObject.AddComponent<RawImage>();

            foreach (var (name, cell) in WorldLayout.Landmarks()) Label(name, cell, 17, new Color(1f, 0.97f, 0.85f));
            foreach (var b in WorldLayout.Buildings)
                if (b.style == BuildingStyle.House)
                    Label(b.hasOwner ? NpcDatabase.FirstName(b.owner) : "", new Vector2(b.rect.x + b.rect.width / 2f, b.rect.y + b.rect.height / 2f), 13, new Color(0.25f, 0.1f, 0.15f));
            Label("YOUR HOME", new Vector2(9f, 44f), 15, new Color(1f, 1f, 1f));

            foreach (var npc in NpcDatabase.All)
            {
                var dot = UIFactory.Panel(frame, "Npc_" + npc.id, Color.yellow, false);
                dot.sprite = SpriteFactory.Circle;
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = Vector2.zero;
                dot.rectTransform.sizeDelta = new Vector2(16, 16);
                dot.gameObject.AddComponent<Outline>().effectColor = Color.black;
                npcDots[npc.id] = dot;
            }

            var obj = UIFactory.Icon(frame, SpriteFactory.ObjectiveArrow, 30);
            objectiveMarker = obj.rectTransform;
            objectiveMarker.anchorMin = objectiveMarker.anchorMax = Vector2.zero;
            objectiveMarker.pivot = new Vector2(0.5f, 0f);
            objectiveMarker.sizeDelta = new Vector2(26, 31);

            var pm = UIFactory.Panel(frame, "Player", new Color(1f, 0.15f, 0.35f), false);
            pm.sprite = SpriteFactory.Circle;
            playerMarker = pm.rectTransform;
            playerMarker.sizeDelta = new Vector2(22, 22);
            playerMarker.anchorMin = playerMarker.anchorMax = Vector2.zero;
            playerMarker.gameObject.AddComponent<Outline>().effectColor = Color.white;

            legend = UIFactory.Label(body, "", 20, TextAnchor.LowerLeft, Theme.TextDim);
            legend.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 66));
        }

        void Label(string text, Vector2 cell, int size, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            var t = UIFactory.Label(frame, text, size, TextAnchor.MiddleCenter, color, FontStyle.Bold);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
            t.rectTransform.sizeDelta = new Vector2(260, 26);
            t.rectTransform.anchoredPosition = cell * Scale;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var s = t.gameObject.AddComponent<Outline>();
            s.effectColor = new Color(0, 0, 0, 0.75f);
        }

        public override void OnOpen()
        {
            if (tex == null) tex = MapTexture.Create(gm.Map);
            MapTexture.Paint(tex, gm.Map);
            mapImage.texture = tex;
            Refresh();
        }

        void Update()
        {
            if (gm != null && gm.InWorld && gm.Player != null) Refresh();
        }

        void Refresh()
        {
            playerMarker.anchoredPosition = (Vector2)gm.Player.transform.position * Scale;
            playerMarker.SetAsLastSibling();
            foreach (var npc in gm.Npcs.All)
            {
                var dot = npcDots[npc.Def.id];
                dot.enabled = !npc.Hidden;
                dot.rectTransform.anchoredPosition = (Vector2)npc.transform.position * Scale;
                dot.color = Guidance.WantsToTalk(gm, npc.Def.id) ? new Color(1f, 0.5f, 0f) : new Color(1f, 0.95f, 0.4f);
            }
            var target = Guidance.ObjectiveTarget(gm);
            objectiveMarker.gameObject.SetActive(target.HasValue);
            if (target.HasValue) objectiveMarker.anchoredPosition = target.Value * Scale + new Vector2(0, 6 + Mathf.Sin(Time.unscaledTime * 4f) * 4f);
            legend.text = "<color=#ff2659><b>Red</b></color> = you    <color=#fff266><b>Yellow</b></color> = resident    " +
                          "<color=#ff8000><b>Orange</b></color> = has work for you    <color=#ffc20e><b>Diamond</b></color> = next goal\n" +
                          $"Mission: {gm.Missions.TrackerText()}";
        }
    }
}
