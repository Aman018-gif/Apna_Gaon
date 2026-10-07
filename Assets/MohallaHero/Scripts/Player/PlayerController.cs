using UnityEngine;

namespace MohallaHero
{
    /// <summary>Top-down movement (keyboard or touch joystick), facing, walk animation and the Interact action.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool IsMoving { get; private set; }
        public Interactable Focus { get; private set; }
        public bool Frozen;   // scripted moments (voting booth, cut-scenes)

        Rigidbody2D rb;
        SpriteRenderer body, candle;
        GlowLight candleGlow;
        Sprite[,] frames;
        float animTime;

        public static PlayerController Spawn(Transform parent, Vector2 position, Gender gender)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.28f;
            col.offset = new Vector2(0, 0.25f);
            var pc = go.AddComponent<PlayerController>();
            pc.Init(gender);
            return pc;
        }

        void Init(Gender gender)
        {
            rb = GetComponent<Rigidbody2D>();
            WorldObjects.AddShadow(transform, 0.9f);
            body = WorldObjects.CreateSprite("Body", null, transform.position, transform);
            frames = new Sprite[3, 2];
            for (int d = 0; d < 3; d++)
                for (int f = 0; f < 2; f++)
                    frames[d, f] = SpriteFactory.Player(gender, d, f);
            body.sprite = frames[0, 0];
            candle = WorldObjects.CreateSprite("Candle", SpriteFactory.Candle, transform.position, body.transform, 1);
            candle.transform.localPosition = new Vector3(0.45f, 0.55f, 0);
            candle.enabled = false;
            candleGlow = GlowLight.Create(body.transform, new Vector2(0.45f, 0.75f), new Color(1f, 0.7f, 0.3f, 0.4f), 2.6f);
            candleGlow.Flicker = true;
            candleGlow.On = false;
        }

        public void SetCandle(bool on)
        {
            if (candle.enabled == on) return;
            candle.enabled = on;
            candleGlow.On = on;
        }

        public void Teleport(Vector2 pos)
        {
            rb.position = pos;
            transform.position = pos;
            rb.SetVelocity(Vector2.zero);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.InWorld) return;
            bool blocked = gm.IsModalOpen || Frozen;

            Vector2 move = blocked ? Vector2.zero : GameInput.Move();
            IsMoving = move.sqrMagnitude > 0.01f;
            if (IsMoving)
            {
                if (Mathf.Abs(move.x) > Mathf.Abs(move.y)) Facing = new Vector2(Mathf.Sign(move.x), 0);
                else Facing = new Vector2(0, Mathf.Sign(move.y));
            }

            bool running = IsMoving && (GameInput.Held(GameKey.Run) || GameInput.VirtualRun || gm.Settings.AlwaysRun);
            float speed = running ? Balance.RunSpeed : Balance.WalkSpeed;
            rb.SetVelocity(move * speed);

            Animate(running);
            UpdateFocus();

            if (blocked) return;
            if (GameInput.Down(GameKey.Interact) && Focus != null && !gm.UI.ClosedThisFrame) Focus.Interact(this);
        }

        void Animate(bool running)
        {
            int dir = Facing.y > 0.5f ? 1 : Mathf.Abs(Facing.x) > 0.5f ? 2 : 0;
            body.flipX = Facing.x < -0.5f;
            int frame = 0;
            if (IsMoving)
            {
                animTime += Time.deltaTime * (running ? 12f : 8f);
                frame = Mathf.FloorToInt(animTime) % 2;
            }
            body.sprite = frames[dir, frame];
            float bob = IsMoving ? Mathf.Abs(Mathf.Sin(animTime * Mathf.PI)) * 0.06f : 0f;
            body.transform.localPosition = new Vector3(0, bob, 0);
        }

        /// <summary>Nearest interactable in front of the player (or very close), within its radius.</summary>
        void UpdateFocus()
        {
            Vector2 probe = (Vector2)transform.position + new Vector2(0, 0.3f) + Facing * 0.55f;
            Interactable best = null;
            float bestD = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || !it.CanInteract) continue;
                float d = Vector2.Distance(probe, it.InteractPoint);
                if (d > it.Radius || d >= bestD) continue;
                best = it;
                bestD = d;
            }
            Focus = best;
        }
    }
}
