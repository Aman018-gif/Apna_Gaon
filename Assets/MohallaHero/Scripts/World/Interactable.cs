using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>Anything the player can use with the Interact key / button. The player picks the nearest one in front.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public virtual float Radius => 1.2f;
        public virtual bool CanInteract => true;
        public virtual Vector2 InteractPoint => transform.position;
        public abstract string Prompt { get; }
        public abstract void Interact(PlayerController player);

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
    }

    public static class WorldObjects
    {
        /// <summary>Creates a GameObject with a SpriteRenderer. Objects share sorting order 0 and are Y-sorted by the camera.</summary>
        public static SpriteRenderer CreateSprite(string name, Sprite sprite, Vector2 position, Transform parent, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            if (order >= 30) Gfx.MakeUnlit(sr);   // markers, bunting and sign plates stay readable at night
            return sr;
        }

        public static T Create<T>(string name, Vector2 position, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.AddComponent<T>();
        }

        public static SpriteRenderer AddShadow(Transform parent, float scale = 1f)
        {
            var sr = CreateSprite("Shadow", SpriteFactory.Shadow, parent.position, parent, -1);
            sr.transform.localPosition = new Vector3(0, 0.02f, 0);
            sr.transform.localScale = Vector3.one * scale;
            return sr;
        }

        /// <summary>A warm glow (lamps, diyas, lit windows): a 2D light with URP, a soft sprite otherwise.</summary>
        public static GlowLight AddGlow(Transform parent, Vector2 offset, Color color, float size) => GlowLight.Create(parent, offset, color, size);

        /// <summary>
        /// World-space text (shop signboards, stall names) using the built-in font, on a dark rounded nameplate
        /// (sized from the font's glyph advances) so it stays readable on any wall or awning.
        /// </summary>
        public static TextMesh Label(Transform parent, string text, Vector2 position, Color color, float size = 0.11f, int order = 35, bool plate = true)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.font = UIFactory.Font;
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UIFactory.Font.material;
            mr.sortingOrder = order;
            if (plate)
            {
                var font = UIFactory.Font;
                font.RequestCharactersInTexture(text, tm.fontSize);
                float width = 0f;
                foreach (char ch in text)
                    if (font.GetCharacterInfo(ch, out var info, tm.fontSize)) width += info.advance;
                float unit = tm.characterSize * 0.1f;   // TextMesh: one font pixel = characterSize / 10 world units
                var bg = CreateSprite("Plate", SpriteFactory.RoundedRect, position, go.transform, order - 1);
                bg.drawMode = SpriteDrawMode.Sliced;
                bg.size = new Vector2(width * unit + 0.35f, tm.fontSize * unit * 1.25f);
                bg.color = new Color(0.17f, 0.07f, 0.33f, 0.88f);
            }
            return tm;
        }
    }

    /// <summary>
    /// A world object that runs an Ink knot when used: wasted lights, leaking taps, saplings, garbage heaps, doors,
    /// stalls, the polling booth... The story calls <c>resolve()</c> to mark it as dealt with (story variable
    /// <c>obj_&lt;id&gt;</c>), which swaps its sprite or hides it, and is saved like any other variable.
    /// </summary>
    public class StoryObject : Interactable
    {
        public string Id;
        public string Knot;
        public string PromptText;
        public float InteractRadius = 1.3f;
        public Vector2 InteractOffset;
        public bool HideWhenResolved;
        public bool StayUsableWhenResolved;
        public Sprite NormalSprite, ResolvedSprite;
        public System.Func<bool> Visible;              // optional: hide entirely (e.g. vote flags before the election)
        public System.Action<StoryObject> OnRefresh;   // optional extra visuals
        public SpriteRenderer Body { get; private set; }

        public string Var => "obj_" + Id;
        public bool Resolved => GameManager.I != null && GameManager.I.Story != null && GameManager.I.Story.Is(Var);

        public override float Radius => InteractRadius;
        public override Vector2 InteractPoint => (Vector2)transform.position + InteractOffset;
        public override bool CanInteract => (StayUsableWhenResolved || !Resolved) && (Visible == null || Visible());
        public override string Prompt => PromptText;

        public static StoryObject Create(Transform parent, string id, string knot, string prompt, Vector2 position, Sprite sprite, Sprite resolved = null)
        {
            var o = WorldObjects.Create<StoryObject>("Obj_" + id, position, parent);
            o.Id = id;
            o.Knot = knot;
            o.PromptText = prompt;
            o.NormalSprite = sprite;
            o.ResolvedSprite = resolved;
            if (sprite != null) o.Body = WorldObjects.CreateSprite("Body", sprite, position, o.transform);
            return o;
        }

        public override void Interact(PlayerController player) => GameManager.I.Dialogue.RunObject(this);

        public void Resolve()
        {
            GameManager.I.Story.Set(Var, 1);
            Refresh();
        }

        public void Refresh()
        {
            bool visible = Visible == null || Visible();
            bool resolved = Resolved;
            if (Body != null)
            {
                Body.enabled = visible && !(HideWhenResolved && resolved);
                if (resolved && ResolvedSprite != null) Body.sprite = ResolvedSprite;
                else if (NormalSprite != null) Body.sprite = NormalSprite;
            }
            OnRefresh?.Invoke(this);
        }
    }
}
