using UnityEngine;

namespace MohallaHero
{
    /// <summary>Base class for NPC finite-state-machine states.</summary>
    public abstract class NPCState
    {
        public abstract string Name { get; }
        public virtual void Enter(NPC npc) { }
        public virtual void Tick(NPC npc, float dt) { }
        public virtual void Exit(NPC npc) { }
    }

    public class NPCStateMachine
    {
        readonly NPC owner;
        public NPCState Current { get; private set; }

        public NPCStateMachine(NPC owner) { this.owner = owner; }

        public void Change(NPCState next)
        {
            Current?.Exit(owner);
            Current = next;
            Current?.Enter(owner);
        }

        public void Tick(float dt) => Current?.Tick(owner, dt);
    }

    /// <summary>Walk to a cell with A*, then switch to the follow-up state. Used for GoToWork / GoHome / GoToSpot.</summary>
    public class GoToState : NPCState
    {
        readonly string name;
        readonly Vector2Int target;
        readonly System.Func<NPCState> onArrive;
        float repathTimer;

        public GoToState(string name, Vector2Int target, System.Func<NPCState> onArrive)
        {
            this.name = name;
            this.target = target;
            this.onArrive = onArrive;
        }

        public override string Name => name;

        public override void Enter(NPC npc)
        {
            npc.SetHidden(false);
            if (!npc.PathTo(target)) npc.Teleport(target);   // unreachable (e.g. behind a barricade) → snap
        }

        public override void Tick(NPC npc, float dt)
        {
            if (npc.FollowPath(dt))
            {
                npc.FSM.Change(onArrive());
                return;
            }
            repathTimer += dt;
            if (repathTimer > 8f)   // stuck (player in the way?) → re-plan
            {
                repathTimer = 0f;
                if (!npc.PathTo(target)) npc.Teleport(target);
            }
        }
    }

    /// <summary>Working: wander inside the work radius and pause (looks busy).</summary>
    public class WorkState : NPCState
    {
        readonly Vector2Int workCell;
        readonly int radius;
        float wait;

        public WorkState(Vector2Int workCell, int radius) { this.workCell = workCell; this.radius = radius; }
        public override string Name => "Work";

        public override void Enter(NPC npc) { npc.SetHidden(false); wait = Random.Range(1f, 4f); }

        public override void Tick(NPC npc, float dt)
        {
            if (npc.HasPath) { npc.FollowPath(dt); return; }
            wait -= dt;
            if (wait > 0f) return;
            wait = Random.Range(4f, 9f);
            if (radius > 0) npc.WanderAround(workCell, radius);
        }
    }

    /// <summary>At home and awake: inside the house (hidden).</summary>
    public class HomeState : NPCState
    {
        public override string Name => "Home";
        public override void Enter(NPC npc) { npc.StopMoving(); npc.SetHidden(true); }
        public override void Exit(NPC npc) => npc.SetHidden(false);
    }

    /// <summary>Asleep at home (hidden, with a "z" bubble over the door).</summary>
    public class SleepState : NPCState
    {
        public override string Name => "Sleep";
        public override void Enter(NPC npc) { npc.StopMoving(); npc.SetHidden(true); }
        public override void Exit(NPC npc) => npc.SetHidden(false);
    }

    /// <summary>Talking with the player: stop and face them until the conversation closes.</summary>
    public class TalkState : NPCState
    {
        readonly Vector2 playerPos;
        public TalkState(Vector2 playerPos) { this.playerPos = playerPos; }
        public override string Name => "Talk";
        public override void Enter(NPC npc) { npc.StopMoving(); npc.FaceTowards(playerPos); }
    }

    /// <summary>Story override: stand at a special spot (the bully corner, a Mela stall, the march start...).</summary>
    public class SpecialState : NPCState
    {
        public readonly Vector2Int Spot;
        public SpecialState(Vector2Int spot) { Spot = spot; }
        public override string Name => "Special";
        public override void Enter(NPC npc)
        {
            npc.SetHidden(false);
            npc.StopMoving();
            npc.FaceTowards((Vector2)npc.transform.position + Vector2.down);
        }
    }

    /// <summary>
    /// Candle march: follow the player's footsteps in a line. Each follower aims for a point further back on the
    /// trail recorded by <see cref="ProtestManager"/>, so the procession bends around corners like a real one.
    /// </summary>
    public class FollowState : NPCState
    {
        readonly int index;
        public FollowState(int index) { this.index = index; }
        public override string Name => "Follow";

        public override void Enter(NPC npc)
        {
            npc.SetHidden(false);
            npc.StopMoving();
            npc.SetMarching(true);
        }

        public override void Exit(NPC npc) => npc.SetMarching(false);

        public override void Tick(NPC npc, float dt)
        {
            var gm = GameManager.I;
            if (gm.Protest == null) return;
            Vector2 goal = gm.Protest.TrailPoint(index);
            float dist = Vector2.Distance(npc.transform.position, goal);
            if (dist < 0.15f) { npc.SetIdle(); npc.FaceTowards(gm.Player.transform.position); return; }
            float speed = Mathf.Lerp(Balance.NpcSpeed, Balance.RunSpeed, Mathf.Clamp01((dist - 1f) / 4f));
            npc.MoveTowards(goal, speed, dt);
        }
    }
}
