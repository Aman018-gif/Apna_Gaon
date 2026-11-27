using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// A resident of Shanti Nagar. A small "brain" picks the desired activity every half second (story position,
    /// following the candle march, work, home, sleep); the FSM executes it, moving along A* paths.
    /// </summary>
    public class NPC : Interactable
    {
        public NpcDef Def { get; private set; }
        public NPCStateMachine FSM { get; private set; }
        public string StateName => FSM?.Current?.Name ?? "-";
        public bool Hidden { get; private set; }
        public bool Talking { get; private set; }

        enum Activity { None, Work, Home, Sleep, Special, Follow }
        Activity activity = Activity.None;
        Vector2Int activityTarget;

        readonly List<Vector2Int> path = new List<Vector2Int>();
        int pathIndex;
        SpriteRenderer body, shadow, marker;
        SpriteRenderer candle;
        GlowLight candleGlow;
        Sprite[,] frames;
        Collider2D col;
        Vector2 facing = Vector2.down;
        float anim, brainTimer, markerTimer;
        bool moving;
        float scale = 1f;

        public Vector2Int Cell => WorldLayout.ToCell(transform.position);
        public bool HasPath => pathIndex < path.Count;

        public void Setup(NpcDef def)
        {
            Def = def;
            FSM = new NPCStateMachine(this);
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.4f);
            box.offset = new Vector2(0, 0.2f);
            col = box;

            scale = def.child ? 0.82f : 1f;
            shadow = WorldObjects.AddShadow(transform, 0.9f * scale);
            body = WorldObjects.CreateSprite("Body", null, transform.position, transform);
            body.transform.localScale = Vector3.one * scale;
            frames = new Sprite[3, 2];
            for (int d = 0; d < 3; d++)
                for (int f = 0; f < 2; f++)
                    frames[d, f] = SpriteFactory.Character(def, d, f);
            body.sprite = frames[0, 0];
            marker = WorldObjects.CreateSprite("Marker", null, transform.position, transform, 30);
            marker.transform.localPosition = new Vector3(0, 1.75f * scale + 0.1f, 0);
            candle = WorldObjects.CreateSprite("Candle", SpriteFactory.Candle, transform.position, body.transform, 1);
            candle.transform.localPosition = new Vector3(0.45f, 0.55f, 0);
            candle.enabled = false;
            candleGlow = GlowLight.Create(body.transform, new Vector2(0.45f, 0.75f), new Color(1f, 0.7f, 0.3f, 0.3f), 2.2f);
            candleGlow.Flicker = true;
            candleGlow.On = false;
        }

        // ------------------------------------------------------------------ Interactable

        public override Vector2 InteractPoint => (Vector2)transform.position + new Vector2(0, 0.4f);
        public override float Radius => 1.3f;
        public override bool CanInteract => !Hidden && activity != Activity.Follow;
        public override string Prompt => $"Baat karo: {NpcDatabase.FirstName(Def.id)}  <size=20><color=#ffe9a8>({Def.role})</color></size>";

        public override void Interact(PlayerController player) => GameManager.I.Dialogue.TalkTo(this);

        public void BeginTalk(Vector2 playerPos)
        {
            Talking = true;
            FSM.Change(new TalkState(playerPos));
        }

        public void EndTalk()
        {
            Talking = false;
            activity = Activity.None;   // force a re-plan
            brainTimer = 0f;
        }

        // ------------------------------------------------------------------ brain

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.InWorld || gm.IsModalOpen) { moving = false; Animate(0f); return; }
            float dt = Time.deltaTime;

            brainTimer -= dt;
            if (brainTimer <= 0f)
            {
                brainTimer = 0.5f;
                Think();
            }
            FSM.Tick(dt);

            markerTimer -= dt;
            if (markerTimer <= 0f) { markerTimer = 0.4f; UpdateMarker(); }
            Animate(dt);
        }

        void Think()
        {
            if (Talking) return;
            Desired(out var act, out var target, out int followIndex);
            if (act == activity && target == activityTarget && FSM.Current != null) return;
            activity = act;
            activityTarget = target;
            var door = Def.Door;
            switch (act)
            {
                case Activity.Work:
                    FSM.Change(new GoToState("GoToWork", target, () => new WorkState(target, Def.workRadius)));
                    break;
                case Activity.Home:
                    FSM.Change(new GoToState("GoHome", door, () => new HomeState()));
                    break;
                case Activity.Sleep:
                    FSM.Change(new GoToState("GoHome", door, () => new SleepState()));
                    break;
                case Activity.Special:
                    FSM.Change(new GoToState("GoToSpot", target, () => new SpecialState(target)));
                    break;
                case Activity.Follow:
                    FSM.Change(new FollowState(followIndex));
                    break;
            }
        }

        void Desired(out Activity act, out Vector2Int target, out int followIndex)
        {
            var gm = GameManager.I;
            followIndex = -1;
            if (gm.Protest != null && gm.Protest.FollowerIndex(Def.id, out followIndex)) { act = Activity.Follow; target = default; return; }
            float h = gm.Clock.HourFloat;
            if (gm.Npcs.TryGetSpecialSpot(Def.id, out target) && h < 22f) { act = Activity.Special; return; }
            if (h >= Def.sleepHour) { act = Activity.Sleep; target = Def.Door; return; }
            if (h >= Def.workStartHour && h < Def.workEndHour) { act = Activity.Work; target = gm.Npcs.WorkCell(Def); return; }
            act = Activity.Home;
            target = Def.Door;
        }

        /// <summary>Puts the NPC directly where its schedule says it should be (used at load, every morning, story jumps).</summary>
        public void PlaceBySchedule()
        {
            Talking = false;
            Desired(out var act, out var target, out int followIndex);
            activity = act;
            activityTarget = target;
            if (act == Activity.Follow) { FSM.Change(new FollowState(followIndex)); return; }
            Teleport(target);
            switch (act)
            {
                case Activity.Work: FSM.Change(new WorkState(target, Def.workRadius)); break;
                case Activity.Home: FSM.Change(new HomeState()); break;
                case Activity.Sleep: FSM.Change(new SleepState()); break;
                default: FSM.Change(new SpecialState(target)); break;
            }
        }

        // ------------------------------------------------------------------ movement helpers (used by states)

        public bool PathTo(Vector2Int target)
        {
            var gm = GameManager.I;
            var start = gm.Nav.NearestWalkable(Cell, 3);
            path.Clear();
            pathIndex = 0;
            if (!AStar.FindPath(gm.Nav, start, target, path)) return false;
            pathIndex = path.Count > 1 ? 1 : 0;
            return true;
        }

        /// <summary>Advances along the path. Returns true when the destination is reached.</summary>
        public bool FollowPath(float dt)
        {
            if (!HasPath) { moving = false; return true; }
            Vector2 goal = WorldLayout.CellFeet(path[pathIndex]);
            if (MoveTowards(goal, Balance.NpcSpeed, dt)) pathIndex++;
            if (!HasPath) { moving = false; return true; }
            return false;
        }

        /// <summary>Moves straight towards a point. Returns true when it is reached.</summary>
        public bool MoveTowards(Vector2 goal, float speed, float dt)
        {
            Vector2 pos = transform.position;
            Vector2 delta = goal - pos;
            float step = speed * dt;
            if (delta.sqrMagnitude > 0.0001f) facing = delta.normalized;
            if (delta.magnitude <= step)
            {
                transform.position = goal;
                moving = delta.magnitude > 0.01f;
                return true;
            }
            transform.position = pos + delta.normalized * step;
            moving = true;
            return false;
        }

        public void SetIdle() => moving = false;

        public void WanderAround(Vector2Int anchor, int radius)
        {
            var gm = GameManager.I;
            for (int i = 0; i < 6; i++)
            {
                var c = anchor + new Vector2Int(Random.Range(-radius, radius + 1), Random.Range(-radius, radius + 1));
                if (!gm.Nav.IsWalkable(c)) continue;
                PathTo(c);
                return;
            }
        }

        public void StopMoving() { path.Clear(); pathIndex = 0; moving = false; }

        public void Teleport(Vector2Int cell) => TeleportTo(WorldLayout.CellFeet(cell));

        public void TeleportTo(Vector2 pos)
        {
            StopMoving();
            transform.position = pos;
        }

        public void FaceTowards(Vector2 p)
        {
            var d = p - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.001f) facing = d.normalized;
        }

        public void SetHidden(bool hidden)
        {
            Hidden = hidden;
            body.enabled = !hidden;
            shadow.enabled = !hidden;
            col.enabled = !hidden;
            if (hidden) marker.sprite = null;
        }

        /// <summary>Walking in the march: no collisions (so the line can pass through people) and a candle in hand.</summary>
        public void SetMarching(bool on)
        {
            col.enabled = !on && !Hidden;
            candle.enabled = on;
            candleGlow.On = on;
        }

        // ------------------------------------------------------------------ visuals

        void UpdateMarker()
        {
            if (Hidden) { marker.sprite = StateName == "Sleep" ? SpriteFactory.Bubble("z") : null; return; }
            marker.sprite = activity != Activity.Follow && Guidance.WantsToTalk(GameManager.I, Def.id) ? SpriteFactory.Bubble("!") : null;
        }

        void Animate(float dt)
        {
            if (body == null) return;
            int dir = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? 2 : facing.y > 0 ? 1 : 0;
            body.flipX = dir == 2 && facing.x < 0;
            int frame = 0;
            if (moving)
            {
                anim += dt * 7f;
                frame = Mathf.FloorToInt(anim) % 2;
            }
            body.sprite = frames[dir, frame];
            body.transform.localPosition = new Vector3(0, moving ? Mathf.Abs(Mathf.Sin(anim * Mathf.PI)) * 0.05f : 0f, 0);
            if (marker.sprite != null)
                marker.transform.localPosition = new Vector3(0, 1.75f * scale + 0.1f + Mathf.Sin(Time.time * 3f) * 0.06f, 0);
        }
    }
}
